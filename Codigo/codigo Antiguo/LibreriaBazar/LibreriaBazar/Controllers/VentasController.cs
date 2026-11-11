using System.Security.Claims;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.Services.Pagos;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class VentasController : Controller
{
    private readonly AppDbContext _db;
    private readonly SelectorMetodoPago _pagos;

    public VentasController(AppDbContext db, SelectorMetodoPago pagos)
    {
        _db = db;
        _pagos = pagos;
    }

    // RF09: historial de ventas
    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-30)).Date;
        var h = (hasta ?? DateTime.Today).Date;

        var ventas = await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Pago)
            .Where(v => v.Fecha >= d && v.Fecha < h.AddDays(1))
            .OrderByDescending(v => v.Fecha)
            .ToListAsync();

        ViewBag.Desde = d.ToString("yyyy-MM-dd");
        ViewBag.Hasta = h.ToString("yyyy-MM-dd");
        return View(ventas);
    }

    public async Task<IActionResult> Details(int id)
    {
        var venta = await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Pago)
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venta == null) return NotFound();
        return View(venta);
    }

    // RF08: registrar una venta con productos, servicios o ambos
    public async Task<IActionResult> Create()
    {
        var vm = new VentaCreateViewModel { Catalogo = await CargarCatalogo() };
        CargarMetodosPago();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VentaCreateViewModel vm)
    {
        CargarMetodosPago();

        // Une las líneas repetidas (mismo producto/servicio) sumando cantidades
        vm.Lineas = (vm.Lineas ?? new List<LineaVentaVm>())
            .Where(l => l.ItemId > 0 && l.Cantidad > 0)
            .GroupBy(l => new { l.Tipo, l.ItemId })
            .Select(g => new LineaVentaVm { Tipo = g.Key.Tipo, ItemId = g.Key.ItemId, Cantidad = g.Sum(x => x.Cantidad) })
            .ToList();

        if (!vm.Lineas.Any())
        {
            ModelState.AddModelError(string.Empty, "Agregue al menos un producto o servicio a la venta.");
        }
        var estrategiaPago = _pagos.Obtener(vm.MetodoPago);
        if (estrategiaPago == null)
        {
            ModelState.AddModelError(nameof(vm.MetodoPago), "Seleccione un método de pago válido.");
        }

        var idsProductos = vm.Lineas.Where(l => l.Tipo == "P").Select(l => l.ItemId).ToList();
        var idsServicios = vm.Lineas.Where(l => l.Tipo == "S").Select(l => l.ItemId).ToList();

        var productos = await _db.Productos.Where(p => idsProductos.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var servicios = await _db.Servicios.Where(s => idsServicios.Contains(s.Id)).ToDictionaryAsync(s => s.Id);

        var venta = new Venta { Fecha = DateTime.Now, UsuarioId = UsuarioActualId() };

        // Los precios y el stock se toman SIEMPRE de la base de datos, no del navegador
        foreach (var linea in vm.Lineas)
        {
            if (linea.Tipo == "P")
            {
                if (!productos.TryGetValue(linea.ItemId, out var p) || !p.Activo)
                {
                    ModelState.AddModelError(string.Empty, "Uno de los productos ya no está disponible.");
                    continue;
                }
                if (p.Stock < linea.Cantidad)
                {
                    ModelState.AddModelError(string.Empty,
                        $"Stock insuficiente de «{p.Nombre}» (disponible: {p.Stock}).");
                    continue;
                }
                venta.Detalles.Add(new DetalleVenta
                {
                    ProductoId = p.Id,
                    Descripcion = p.Nombre,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = p.PrecioVenta,
                    Subtotal = p.PrecioVenta * linea.Cantidad
                });
            }
            else if (linea.Tipo == "S")
            {
                if (!servicios.TryGetValue(linea.ItemId, out var s) || !s.Activo)
                {
                    ModelState.AddModelError(string.Empty, "Uno de los servicios ya no está disponible.");
                    continue;
                }
                venta.Detalles.Add(new DetalleVenta
                {
                    ServicioId = s.Id,
                    Descripcion = s.Nombre,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = s.Precio,
                    Subtotal = s.Precio * linea.Cantidad
                });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Se envió una línea de venta no válida.");
            }
        }

        // RF10 y RF11: total y descuento
        venta.Subtotal = venta.Detalles.Sum(d => d.Subtotal);
        if (vm.Descuento > venta.Subtotal)
        {
            ModelState.AddModelError(nameof(vm.Descuento), "El descuento no puede ser mayor al subtotal de la venta.");
        }
        venta.Descuento = vm.Descuento;
        venta.Total = venta.Subtotal - venta.Descuento;

        // RF12: pago del cliente. La regla (vuelto, monto exacto...) la decide la estrategia del método elegido
        ResultadoPago? resultadoPago = null;
        if (estrategiaPago != null)
        {
            resultadoPago = estrategiaPago.Procesar(venta.Total, vm.MontoRecibido);
            if (!resultadoPago.Exitoso)
            {
                ModelState.AddModelError(nameof(vm.MontoRecibido), resultadoPago.Error!);
            }
        }

        if (!ModelState.IsValid)
        {
            vm.Catalogo = await CargarCatalogo();
            return View(vm);
        }

        venta.Pago = new PagoCliente
        {
            Metodo = estrategiaPago!.Nombre,
            MontoRecibido = resultadoPago!.MontoRecibido,
            Vuelto = resultadoPago.Vuelto,
            Fecha = venta.Fecha
        };

        await using var transaccion = await _db.Database.BeginTransactionAsync();

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();

        // RF13 y RNF04: el stock se actualiza automáticamente
        var productosBajos = new List<string>();
        foreach (var detalle in venta.Detalles.Where(d => d.ProductoId.HasValue))
        {
            var producto = productos[detalle.ProductoId!.Value];
            producto.Stock -= detalle.Cantidad;

            _db.Movimientos.Add(new MovimientoInventario
            {
                Fecha = venta.Fecha,
                ProductoId = producto.Id,
                Tipo = TipoMovimiento.Venta,
                Cantidad = -detalle.Cantidad,
                StockResultante = producto.Stock,
                Motivo = $"Venta #{venta.Id}",
                UsuarioId = venta.UsuarioId
            });

            if (producto.Stock <= producto.StockMinimo)
            {
                productosBajos.Add($"{producto.Nombre} (quedan {producto.Stock})");
            }
        }

        await _db.SaveChangesAsync();
        await transaccion.CommitAsync();

        // RNF16: confirmación; RF23: alerta de stock bajo
        TempData["Ok"] = $"Venta #{venta.Id} registrada correctamente. Total: {venta.Total:C2}.";
        if (productosBajos.Any())
        {
            TempData["Alerta"] = "Stock bajo: " + string.Join(", ", productosBajos) + ".";
        }
        return RedirectToAction(nameof(Details), new { id = venta.Id });
    }

    private void CargarMetodosPago()
    {
        ViewBag.Metodos = _pagos.Nombres;
        ViewBag.MetodosConMonto = _pagos.NombresConMontoRecibido;
    }

    private async Task<List<CatalogoItemVm>> CargarCatalogo()
    {
        var productos = await _db.Productos
            .AsNoTracking()
            .Where(p => p.Activo && p.Stock > 0)
            .OrderBy(p => p.Nombre)
            .Select(p => new CatalogoItemVm { Tipo = "P", Id = p.Id, Nombre = p.Nombre, Precio = p.PrecioVenta, Stock = p.Stock })
            .ToListAsync();

        var servicios = await _db.Servicios
            .AsNoTracking()
            .Where(s => s.Activo)
            .OrderBy(s => s.Nombre)
            .Select(s => new CatalogoItemVm { Tipo = "S", Id = s.Id, Nombre = s.Nombre, Precio = s.Precio, Stock = null })
            .ToListAsync();

        return productos.Concat(servicios).ToList();
    }

    private int UsuarioActualId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
