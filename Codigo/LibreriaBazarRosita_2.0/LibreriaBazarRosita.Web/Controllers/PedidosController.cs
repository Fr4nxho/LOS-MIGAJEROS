
using System.Security.Claims;
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazarRosita.Web.Controllers;

[Authorize(Roles = "PROPIETARIO,ENCARGADO")]
public class PedidosController : Controller
{
    private readonly LibreriaBazarRositaContext _context;

    public PedidosController(
        LibreriaBazarRositaContext context)
    {
        _context = context;
    }

    // ========================================
    // CONSULTAR PEDIDOS
    // ========================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pedidos = await _context.PedidoProveedors
            .AsNoTracking()
            .Include(p => p.IdProveedorNavigation)
            .Include(p => p.DetallePedidos)
            .OrderByDescending(p => p.Fecha)
            .ThenByDescending(p => p.IdPedido)
            .ToListAsync();

        return View(pedidos);
    }

    // ========================================
    // MOSTRAR FORMULARIO
    // ========================================

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarOpciones();

        return View(new CrearPedidoViewModel());
    }

    // ========================================
    // REGISTRAR PEDIDO
    // ========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        CrearPedidoViewModel modelo)
    {
        modelo.Detalles ??= new List<LineaPedidoViewModel>();

        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int idUsuario))
        {
            return Forbid();
        }

        // Verificar que el proveedor esté activo.
        bool proveedorValido = await _context.Proveedors
            .AnyAsync(p =>
                p.IdProveedor == modelo.IdProveedor &&
                p.Estado);

        if (!proveedorValido)
        {
            ModelState.AddModelError(
                nameof(modelo.IdProveedor),
                "Seleccione un proveedor activo.");
        }

        // Omitir filas completamente vacías.
        var lineas = modelo.Detalles
            .Where(d =>
                d.IdProducto.HasValue ||
                d.Cantidad.HasValue ||
                d.CostoUnitario.HasValue)
            .ToList();

        if (lineas.Count == 0)
        {
            ModelState.AddModelError(
                "",
                "Agregue al menos un producto.");
        }

        if (modelo.Detalles.Count > 20)
        {
            ModelState.AddModelError(
                "",
                "El pedido permite como máximo 20 líneas.");
        }

        // Consultar productos activos.
        var ids = lineas
            .Where(d => d.IdProducto.HasValue)
            .Select(d => d.IdProducto!.Value)
            .Distinct()
            .ToList();

        var productosValidos = await _context.Productos
            .AsNoTracking()
            .Where(p =>
                ids.Contains(p.IdProducto) &&
                p.IdProductoNavigation.Estado &&
                p.IdProductoNavigation.TipoItem == "PRODUCTO")
            .Select(p => p.IdProducto)
            .ToListAsync();

        var idsValidos = productosValidos.ToHashSet();
        var idsRepetidos = new HashSet<int>();

        decimal total = 0m;

        var detallesNuevos = new List<DetallePedido>();

        // Validar cada línea del pedido.
        for (int i = 0; i < modelo.Detalles.Count; i++)
        {
            var linea = modelo.Detalles[i];

            bool vacia =
                !linea.IdProducto.HasValue &&
                !linea.Cantidad.HasValue &&
                !linea.CostoUnitario.HasValue;

            if (vacia)
            {
                continue;
            }

            if (!linea.IdProducto.HasValue ||
                !idsValidos.Contains(linea.IdProducto.Value))
            {
                ModelState.AddModelError(
                    $"Detalles[{i}].IdProducto",
                    "Seleccione un producto activo.");
                continue;
            }

            if (!idsRepetidos.Add(linea.IdProducto.Value))
            {
                ModelState.AddModelError(
                    $"Detalles[{i}].IdProducto",
                    "El producto está repetido en el pedido.");
                continue;
            }

            if (!linea.Cantidad.HasValue ||
                linea.Cantidad.Value <= 0 ||
                linea.Cantidad.Value > 10000)
            {
                ModelState.AddModelError(
                    $"Detalles[{i}].Cantidad",
                    "Ingrese una cantidad entre 1 y 10000.");
                continue;
            }

            if (!linea.CostoUnitario.HasValue ||
                linea.CostoUnitario.Value <= 0 ||
                linea.CostoUnitario.Value > 100000 ||
                decimal.Round(
                    linea.CostoUnitario.Value, 2) !=
                    linea.CostoUnitario.Value)
            {
                ModelState.AddModelError(
                    $"Detalles[{i}].CostoUnitario",
                    "Ingrese un costo válido con máximo 2 decimales.");
                continue;
            }

            decimal subtotal =
                linea.Cantidad.Value *
                linea.CostoUnitario.Value;

            total += subtotal;

            detallesNuevos.Add(new DetallePedido
            {
                IdProducto = linea.IdProducto.Value,
                Cantidad = linea.Cantidad.Value,
                CostoUnitario = linea.CostoUnitario.Value,
                Subtotal = subtotal
            });
        }

        if (total > 9999999999.99m)
        {
            ModelState.AddModelError(
                "",
                "El total excede el importe permitido.");
        }

        if (!ModelState.IsValid ||
            detallesNuevos.Count == 0)
        {
            await CargarOpciones();
            return View(modelo);
        }

        // Crear cabecera y detalles.
        var pedido = new PedidoProveedor
        {
            IdProveedor = modelo.IdProveedor!.Value,
            IdUsuario = idUsuario,
            Fecha = DateTime.Now,
            Estado = "PENDIENTE",
            Total = total,
            DetallePedidos = detallesNuevos
        };

        try
        {
            _context.PedidoProveedors.Add(pedido);

            // EF Core guarda cabecera y detalles
            // en una misma operación transaccional.
            await _context.SaveChangesAsync();

            TempData["Exito"] =
                $"Pedido N.º {pedido.IdPedido} registrado. " +
                $"Total: S/ {pedido.Total:0.00}.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "No se pudo registrar el pedido. " +
                "Compruebe los datos.");

            await CargarOpciones();
            return View(modelo);
        }
    }

    // ========================================
    // CARGAR PROVEEDORES Y PRODUCTOS
    // ========================================

    private async Task CargarOpciones()
    {
        var proveedores = await _context.Proveedors
            .AsNoTracking()
            .Where(p => p.Estado)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        ViewBag.Proveedores = proveedores
            .Select(p => new SelectListItem
            {
                Value = p.IdProveedor.ToString(),
                Text = p.Nombre
            })
            .ToList();

        var productos = await _context.Productos
            .AsNoTracking()
            .Include(p => p.IdProductoNavigation)
            .Where(p =>
                p.IdProductoNavigation.Estado &&
                p.IdProductoNavigation.TipoItem == "PRODUCTO")
            .OrderBy(p => p.IdProductoNavigation.Nombre)
            .ToListAsync();

        ViewBag.Productos = productos
            .Select(p => new SelectListItem
            {
                Value = p.IdProducto.ToString(),
                Text = p.IdProductoNavigation.Nombre
            })
            .ToList();
    }

    // ========================================
    // CONSULTAR DETALLE DEL PEDIDO
    // ========================================

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var pedido = await _context.PedidoProveedors
            .AsNoTracking()
            .Include(p => p.IdProveedorNavigation)
            .Include(p => p.DetallePedidos)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(p => p.IdProductoNavigation)
            .Include(p => p.PagoProveedor)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido == null)
        {
            return NotFound();
        }

        return View(pedido);
    }

    // ========================================
    // MOSTRAR FORMULARIO DE PAGO
    // ========================================

    [HttpGet]
    public async Task<IActionResult> RegistrarPago(int id)
    {
        var pedido = await _context.PedidoProveedors
            .AsNoTracking()
            .Include(p => p.IdProveedorNavigation)
            .Include(p => p.PagoProveedor)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido == null)
            return NotFound();

        if (pedido.PagoProveedor != null)
        {
            TempData["Info"] = "Este pedido ya tiene un pago registrado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (pedido.Estado == "ANULADO")
            return BadRequest("No se puede pagar un pedido anulado.");

        var modelo = new RegistrarPagoProveedorViewModel
        {
            IdPedido = pedido.IdPedido,
            NombreProveedor = pedido.IdProveedorNavigation.Nombre,
            TotalPedido = pedido.Total
        };

        return View(modelo);
    }

    // ========================================
    // GUARDAR PAGO AL PROVEEDOR
    // ========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPago(
        int id,
        RegistrarPagoProveedorViewModel modelo)
    {
        if (id != modelo.IdPedido)
            return BadRequest();

        var pedido = await _context.PedidoProveedors
            .AsNoTracking()
            .Include(p => p.IdProveedorNavigation)
            .Include(p => p.PagoProveedor)
            .FirstOrDefaultAsync(p => p.IdPedido == id);

        if (pedido == null)
            return NotFound();

        if (pedido.PagoProveedor != null)
        {
            TempData["Info"] = "Este pedido ya fue pagado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (pedido.Estado == "ANULADO")
            return BadRequest("No se puede pagar un pedido anulado.");

        modelo.NombreProveedor = pedido.IdProveedorNavigation.Nombre;
        modelo.TotalPedido = pedido.Total;

        modelo.MedioPago = modelo.MedioPago?.Trim().ToUpperInvariant() ?? "";

        string[] mediosPermitidos =
        {
            "EFECTIVO",
            "YAPE",
            "PLIN",
            "TRANSFERENCIA"
        };

        if (!mediosPermitidos.Contains(modelo.MedioPago))
        {
            ModelState.AddModelError(
                nameof(modelo.MedioPago),
                "Seleccione un medio de pago válido.");
        }

        if (!ModelState.IsValid)
            return View(modelo);

        if (pedido.Total <= 0)
        {
            ModelState.AddModelError(
                "",
                "El importe del pedido debe ser mayor que cero.");

            return View(modelo);
        }

        var pago = new PagoProveedor
        {
            IdPedido = pedido.IdPedido,
            FechaHora = DateTime.Now,
            Monto = pedido.Total,
            MedioPago = modelo.MedioPago
        };

        try
        {
            _context.PagoProveedors.Add(pago);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Pago al proveedor registrado correctamente.";

            return RedirectToAction(
                nameof(Detalle),
                new { id = pedido.IdPedido });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "No se pudo registrar el pago. " +
                "Compruebe si el pedido ya fue pagado.");

            return View(modelo);
        }
    }

    // ========================================
    // RECIBIR MERCADERÍA DEL PROVEEDOR
    // ========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(int id)
    {
        if (!int.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out int idUsuario))
        {
            return Forbid();
        }

        // Consultar los productos del pedido.
        var detalles = await _context.DetallePedidos
            .AsNoTracking()
            .Where(d => d.IdPedido == id)
            .ToListAsync();

        if (detalles.Count == 0 ||
            detalles.Any(d => d.Cantidad <= 0))
        {
            TempData["Info"] =
                "El pedido no contiene productos válidos.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }

        // Toda la recepción debe ser atómica.
        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Cambiar el estado únicamente si sigue PENDIENTE.
            // Evita registrar dos veces la misma recepción.
            int pedidosActualizados =
                await _context.PedidoProveedors
                    .Where(p =>
                        p.IdPedido == id &&
                        p.Estado == "PENDIENTE")
                    .ExecuteUpdateAsync(actualizacion =>
                        actualizacion.SetProperty(
                            p => p.Estado,
                            "RECIBIDO"));

            if (pedidosActualizados != 1)
            {
                await transaccion.RollbackAsync();

                TempData["Info"] =
                    "El pedido ya fue recibido o no está pendiente.";

                return RedirectToAction(
                    nameof(Detalle), new { id });
            }

            DateTime fechaRecepcion = DateTime.Now;

            foreach (var detalle in detalles)
            {
                int cantidad = detalle.Cantidad;

                // Sumar al stock existente directamente en SQL.
                // Se verifica que no se produzca desbordamiento.
                int productosActualizados =
                    await _context.Productos
                        .Where(p =>
                            p.IdProducto == detalle.IdProducto &&
                            p.StockActual >= 0 &&
                            p.StockActual <= int.MaxValue - cantidad)
                        .ExecuteUpdateAsync(actualizacion =>
                            actualizacion.SetProperty(
                                p => p.StockActual,
                                p => p.StockActual + cantidad));

                if (productosActualizados != 1)
                {
                    await transaccion.RollbackAsync();

                    TempData["Info"] =
                        "No se pudo actualizar el stock de uno " +
                        "de los productos. No se realizaron cambios.";

                    return RedirectToAction(
                        nameof(Detalle), new { id });
                }

                // Registrar la entrada en el historial.
                var movimiento = new MovimientoInventario
                {
                    IdProducto = detalle.IdProducto,
                    IdUsuario = idUsuario,
                    IdPedido = id,
                    IdVenta = null,
                    FechaHora = fechaRecepcion,
                    TipoMovimiento = "ENTRADA",
                    Cantidad = cantidad,
                    Motivo = "RECEPCION_PEDIDO"
                };

                _context.MovimientoInventarios.Add(movimiento);
            }

            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            TempData["Exito"] =
                "Mercadería recibida e inventario actualizado correctamente.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();

            TempData["Info"] =
                "No se pudo completar la recepción. " +
                "No se confirmaron los cambios.";

            return RedirectToAction(
                nameof(Detalle), new { id });
        }
    }

}
