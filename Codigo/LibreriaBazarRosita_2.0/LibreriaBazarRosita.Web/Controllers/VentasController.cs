
using System.Security.Claims;
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazarRosita.Web.Controllers
{
    [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
    public class VentasController : Controller
    {
        private readonly LibreriaBazarRositaContext _context;
        private readonly ILogger<VentasController> _logger;

        private const decimal MaximoImporte = 9999999999.99m;

        public VentasController(
            LibreriaBazarRositaContext context,
            ILogger<VentasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // 1. MOSTRAR FORMULARIO DE VENTA
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarItemsAsync();

            return View(new RegistrarVentaViewModel());
        }

        // ==========================================
        // 2. REGISTRAR LA VENTA COMPLETA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            RegistrarVentaViewModel modelo)
        {
            // Obtener el usuario autenticado.
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            // Validar medio de pago.
            string medioPago =
                modelo.MedioPago?.Trim().ToUpperInvariant() ?? "";

            if (medioPago is not
                ("EFECTIVO" or "YAPE" or "PLIN" or "TRANSFERENCIA"))
            {
                ModelState.AddModelError(
                    nameof(modelo.MedioPago),
                    "Seleccione un medio de pago válido.");
            }

            // Validar descuento monetario.
            if (modelo.Descuento < 0 ||
                modelo.Descuento > MaximoImporte ||
                decimal.Round(modelo.Descuento, 2) != modelo.Descuento)
            {
                ModelState.AddModelError(
                    nameof(modelo.Descuento),
                    "El descuento debe ser válido y tener " +
                    "como máximo dos decimales.");
            }

            if (modelo.Detalles == null ||
                modelo.Detalles.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "Agregue al menos un producto o servicio.");

                return await MostrarFormulario(modelo);
            }

            if (modelo.Detalles.Count > 100)
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "La venta supera la cantidad máxima de líneas.");
            }

            if (!ModelState.IsValid)
            {
                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 3. AGRUPAR ÍTEMS REPETIDOS
            // ======================================

            // Ejemplo:
            // Cuaderno A4 x2 + Cuaderno A4 x3
            // Se procesa como Cuaderno A4 x5.

            var agrupados = modelo.Detalles
                .GroupBy(d => d.IdItem)
                .Select(g => new
                {
                    IdItem = g.Key,
                    Cantidad = g.Sum(d => (long)d.Cantidad)
                })
                .ToList();

            if (agrupados.Any(d =>
                d.IdItem <= 0 ||
                d.Cantidad <= 0 ||
                d.Cantidad > int.MaxValue))
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "Existen artículos o cantidades inválidas.");

                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 4. CONSULTAR PRECIOS REALES
            // ======================================

            var ids = agrupados
                .Select(d => d.IdItem)
                .ToList();

            var items = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .Include(i => i.Servicio)
                .Where(i => ids.Contains(i.IdItem))
                .ToDictionaryAsync(i => i.IdItem);

            var lineas = new List<(
                int IdItem,
                string Nombre,
                int Cantidad,
                decimal Precio,
                decimal Subtotal,
                bool EsProducto)>();

            decimal subtotalVenta = 0m;

            foreach (var detalle in agrupados)
            {
                if (!items.TryGetValue(
                    detalle.IdItem,
                    out var item))
                {
                    ModelState.AddModelError(
                        "",
                        $"No existe el ítem {detalle.IdItem}.");

                    continue;
                }

                if (!item.Estado)
                {
                    ModelState.AddModelError(
                        "",
                        $"El ítem {item.Nombre} está desactivado.");

                    continue;
                }

                bool esProducto = item.TipoItem == "PRODUCTO";
                bool esServicio = item.TipoItem == "SERVICIO";

                if ((!esProducto && !esServicio) ||
                    (esProducto && item.Producto == null) ||
                    (esServicio && item.Servicio == null))
                {
                    ModelState.AddModelError(
                        "",
                        $"El ítem {item.Nombre} no tiene " +
                        "una configuración válida.");

                    continue;
                }

                int cantidad = (int)detalle.Cantidad;

                // Solo los productos necesitan existencias.
                if (esProducto &&
                    item.Producto!.StockActual < cantidad)
                {
                    ModelState.AddModelError(
                        "",
                        $"Stock insuficiente para {item.Nombre}. " +
                        $"Disponible: {item.Producto.StockActual}.");

                    continue;
                }

                if (item.Precio < 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"El precio de {item.Nombre} no es válido.");

                    continue;
                }

                decimal subtotalLinea;

                try
                {
                    subtotalLinea = checked(
                        item.Precio * cantidad);
                }
                catch (OverflowException)
                {
                    ModelState.AddModelError(
                        "",
                        $"El importe de {item.Nombre} es demasiado alto.");

                    continue;
                }

                if (subtotalLinea > MaximoImporte ||
                    subtotalVenta > MaximoImporte - subtotalLinea)
                {
                    ModelState.AddModelError(
                        "",
                        "El importe de la venta excede el máximo permitido.");

                    continue;
                }

                subtotalVenta += subtotalLinea;

                lineas.Add((
                    item.IdItem,
                    item.Nombre,
                    cantidad,
                    item.Precio,
                    subtotalLinea,
                    esProducto
                ));
            }

            if (!ModelState.IsValid)
            {
                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 5. CALCULAR TOTAL DEFINITIVO
            // ======================================

            if (modelo.Descuento > subtotalVenta)
            {
                ModelState.AddModelError(
                    nameof(modelo.Descuento),
                    "El descuento no puede superar el subtotal.");

                return await MostrarFormulario(modelo);
            }

            decimal totalVenta =
                subtotalVenta - modelo.Descuento;

            DateTime fechaHora = DateTime.Now;

            // ======================================
            // 6. INICIAR TRANSACCIÓN
            // ======================================

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // PASO A: Crear venta principal.
                var venta = new Ventum
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaHora,
                    Subtotal = subtotalVenta,
                    Descuento = modelo.Descuento,
                    Total = totalVenta,
                    Estado = "COMPLETADA"
                };

                _context.Set<Ventum>().Add(venta);

                // Obtener el IdVenta generado por SQL Server.
                await _context.SaveChangesAsync();

                // PASO B: Crear detalles.
                foreach (var linea in lineas)
                {
                    _context.Set<DetalleVentum>().Add(
                        new DetalleVentum
                        {
                            IdVenta = venta.IdVenta,
                            IdItem = linea.IdItem,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.Precio,
                            Subtotal = linea.Subtotal
                        });
                }

                // PASO C: Registrar pago del cliente.
                _context.Set<PagoVentum>().Add(
                    new PagoVentum
                    {
                        IdVenta = venta.IdVenta,
                        FechaHora = fechaHora,
                        Monto = totalVenta,
                        MedioPago = medioPago
                    });

                // PASO D: Actualizar stock de productos.
                foreach (var linea in lineas.Where(l => l.EsProducto))
                {
                    // Condición importante:
                    // Solo descontar si sigue existiendo
                    // suficiente stock al confirmar la venta.
                    int filasActualizadas =
                        await _context.Set<Producto>()
                            .Where(p =>
                                p.IdProducto == linea.IdItem &&
                                p.StockActual >= linea.Cantidad)
                            .ExecuteUpdateAsync(actualizacion =>
                                actualizacion.SetProperty(
                                    p => p.StockActual,
                                    p => p.StockActual - linea.Cantidad));

                    if (filasActualizadas != 1)
                    {
                        // Deshacer toda la venta.
                        await transaccion.RollbackAsync();

                        ModelState.AddModelError(
                            "",
                            $"El stock de {linea.Nombre} cambió " +
                            "durante el registro. Intente nuevamente.");

                        return await MostrarFormulario(modelo);
                    }

                    // PASO E: Registrar movimiento de salida.
                    _context.Set<MovimientoInventario>().Add(
                        new MovimientoInventario
                        {
                            IdProducto = linea.IdItem,
                            IdUsuario = idUsuario,
                            IdVenta = venta.IdVenta,
                            IdPedido = null,
                            FechaHora = fechaHora,
                            TipoMovimiento = "SALIDA",
                            Cantidad = -linea.Cantidad,
                            Motivo = "VENTA"
                        });
                }

                // Guardar detalles, pago y movimientos.
                await _context.SaveChangesAsync();

                // PASO F: Confirmar todos los cambios.
                await transaccion.CommitAsync();

                TempData["Exito"] =
                    $"Venta N.º {venta.IdVenta} registrada correctamente. " +
                    $"Total: S/ {totalVenta:0.00}.";

                return RedirectToAction(nameof(Crear));
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error al registrar venta del usuario {IdUsuario}",
                    idUsuario);

                ModelState.AddModelError(
                    "",
                    "No se pudo completar la venta. " +
                    "No se confirmaron cambios en la base de datos.");

                return await MostrarFormulario(modelo);
            }
        }

        // ==========================================
        // 7. CARGAR CATÁLOGO DISPONIBLE
        // ==========================================

        private async Task CargarItemsAsync()
        {
            ViewBag.Items = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .Include(i => i.Servicio)
                .Where(i =>
                    i.Estado &&
                    (
                        (i.TipoItem == "SERVICIO" &&
                         i.Servicio != null)
                        ||
                        (i.TipoItem == "PRODUCTO" &&
                         i.Producto != null &&
                         i.Producto.StockActual > 0)
                    ))
                .OrderBy(i => i.Nombre)
                .ToListAsync();
        }

        // ==========================================
        // 8. REGRESAR AL FORMULARIO CON ERRORES
        // ==========================================

        private async Task<IActionResult> MostrarFormulario(
            RegistrarVentaViewModel modelo)
        {
            await CargarItemsAsync();

            return View("Crear", modelo);
        }
    }
}
