using System.Security.Claims;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

// Abastecimiento: pedidos a proveedores (RF21), pagos (RF22) y recepción de mercadería (RF15)
public class PedidosController : Controller
{
    private static readonly string[] MetodosPago = { "Efectivo", "Transferencia", "Yape / Plin", "Tarjeta" };

    private readonly AppDbContext _db;

    public PedidosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var pedidos = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Proveedor)
            .Include(p => p.Pagos)
            .OrderByDescending(p => p.Fecha)
            .Take(200)
            .ToListAsync();
        return View(pedidos);
    }

    public async Task<IActionResult> Details(int id)
    {
        var pedido = await _db.Pedidos
            .AsNoTracking()
            .Include(p => p.Proveedor)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .Include(p => p.Pagos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pedido == null) return NotFound();

        ViewBag.Metodos = MetodosPago;
        return View(pedido);
    }

    public async Task<IActionResult> Create(int? productoId)
    {
        var vm = new PedidoCreateViewModel();
        await CargarListas(vm);
        if (productoId.HasValue)
        {
            var p = vm.Productos.FirstOrDefault(x => x.Id == productoId.Value);
            if (p != null)
            {
                vm.Lineas.Add(new LineaPedidoVm { ProductoId = p.Id, Cantidad = Math.Max(1, p.StockMinimo * 2 - p.Stock), CostoUnitario = p.CostoUnitario });
            }
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PedidoCreateViewModel vm)
    {
        vm.Lineas = (vm.Lineas ?? new List<LineaPedidoVm>())
            .Where(l => l.ProductoId > 0 && l.Cantidad > 0)
            .GroupBy(l => l.ProductoId)
            .Select(g => new LineaPedidoVm
            {
                ProductoId = g.Key,
                Cantidad = g.Sum(x => x.Cantidad),
                CostoUnitario = g.First().CostoUnitario
            })
            .ToList();

        if (!vm.Lineas.Any())
        {
            ModelState.AddModelError(string.Empty, "Agregue al menos un producto al pedido.");
        }
        if (vm.Lineas.Any(l => l.CostoUnitario < 0))
        {
            ModelState.AddModelError(string.Empty, "El costo unitario no puede ser negativo.");
        }

        var proveedor = await _db.Proveedores.FindAsync(vm.ProveedorId);
        if (proveedor == null || !proveedor.Activo)
        {
            ModelState.AddModelError(nameof(vm.ProveedorId), "Seleccione un proveedor activo.");
        }

        var ids = vm.Lineas.Select(l => l.ProductoId).ToList();
        var productos = await _db.Productos.Where(p => ids.Contains(p.Id) && p.Activo).ToDictionaryAsync(p => p.Id);
        if (ids.Any(id => !productos.ContainsKey(id)))
        {
            ModelState.AddModelError(string.Empty, "Uno de los productos del pedido no está disponible.");
        }

        if (!ModelState.IsValid)
        {
            await CargarListas(vm);
            return View(vm);
        }

        var pedido = new PedidoProveedor
        {
            ProveedorId = vm.ProveedorId,
            Fecha = DateTime.Now,
            Estado = EstadoPedido.Pendiente,
            Observacion = vm.Observacion
        };
        foreach (var l in vm.Lineas)
        {
            pedido.Detalles.Add(new DetallePedido
            {
                ProductoId = l.ProductoId,
                Cantidad = l.Cantidad,
                CostoUnitario = l.CostoUnitario,
                Subtotal = l.CostoUnitario * l.Cantidad
            });
        }
        pedido.Total = pedido.Detalles.Sum(d => d.Subtotal);

        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Pedido #{pedido.Id} registrado para {proveedor!.Nombre}. Total: {pedido.Total:C2}.";
        return RedirectToAction(nameof(Details), new { id = pedido.Id });
    }

    // Recepción de la mercadería: actualiza el stock automáticamente
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recibir(int id)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null) return NotFound();

        if (pedido.Estado != EstadoPedido.Pendiente)
        {
            TempData["Error"] = "Solo se puede recibir un pedido que esté pendiente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var detalle in pedido.Detalles)
        {
            var producto = await _db.Productos.FindAsync(detalle.ProductoId);
            if (producto == null) continue;

            producto.Stock += detalle.Cantidad;
            if (detalle.CostoUnitario > 0)
            {
                producto.CostoUnitario = detalle.CostoUnitario;
            }

            _db.Movimientos.Add(new MovimientoInventario
            {
                ProductoId = producto.Id,
                Tipo = TipoMovimiento.RecepcionPedido,
                Cantidad = detalle.Cantidad,
                StockResultante = producto.Stock,
                Motivo = $"Recepción del pedido #{pedido.Id}",
                UsuarioId = UsuarioActualId(),
                ProveedorId = pedido.ProveedorId
            });
        }

        pedido.Estado = EstadoPedido.Recibido;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Pedido #{pedido.Id} recibido. El stock fue actualizado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var pedido = await _db.Pedidos.FindAsync(id);
        if (pedido == null) return NotFound();

        if (pedido.Estado != EstadoPedido.Pendiente)
        {
            TempData["Error"] = "Solo se puede cancelar un pedido que esté pendiente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        pedido.Estado = EstadoPedido.Cancelado;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Pedido #{pedido.Id} cancelado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // RF22: pagos a proveedores
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarPago(PagoProveedorViewModel vm)
    {
        var pedido = await _db.Pedidos
            .Include(p => p.Pagos)
            .FirstOrDefaultAsync(p => p.Id == vm.PedidoId);
        if (pedido == null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = vm.PedidoId });
        }
        if (pedido.Estado == EstadoPedido.Cancelado)
        {
            TempData["Error"] = "No se pueden registrar pagos en un pedido cancelado.";
            return RedirectToAction(nameof(Details), new { id = vm.PedidoId });
        }
        if (vm.Monto > pedido.Saldo)
        {
            TempData["Error"] = $"El monto supera el saldo pendiente ({pedido.Saldo:C2}).";
            return RedirectToAction(nameof(Details), new { id = vm.PedidoId });
        }

        _db.PagosProveedor.Add(new PagoProveedor
        {
            PedidoProveedorId = pedido.Id,
            Fecha = DateTime.Now,
            Monto = vm.Monto,
            Metodo = MetodosPago.Contains(vm.Metodo) ? vm.Metodo : "Efectivo",
            Observacion = vm.Observacion
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Pago de {vm.Monto:C2} registrado en el pedido #{pedido.Id}.";
        return RedirectToAction(nameof(Details), new { id = vm.PedidoId });
    }

    private async Task CargarListas(PedidoCreateViewModel vm)
    {
        vm.Productos = await _db.Productos.AsNoTracking().Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Proveedores = new SelectList(
            await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(),
            "Id", "Nombre", vm.ProveedorId);
    }

    private int UsuarioActualId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
