using System.Security.Claims;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class InventarioController : Controller
{
    private readonly AppDbContext _db;

    public InventarioController(AppDbContext db) => _db = db;

    // RF14 y RF23: stock disponible y alertas de stock bajo
    public async Task<IActionResult> Index(string? buscar, bool soloBajoStock = false)
    {
        var consulta = _db.Productos.Include(p => p.Categoria).AsNoTracking().Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(p =>
                EF.Functions.Collate(p.Nombre, "Latin1_General_CI_AI").Contains(termino));
        }
        if (soloBajoStock)
        {
            consulta = consulta.Where(p => p.Stock <= p.StockMinimo);
        }

        ViewBag.Buscar = buscar;
        ViewBag.SoloBajoStock = soloBajoStock;
        return View(await consulta.OrderBy(p => p.Nombre).ToListAsync());
    }

    // RF15: ingreso de nueva mercadería
    public async Task<IActionResult> Ingreso(int? productoId)
    {
        await CargarListas(productoId);
        return View(new IngresoViewModel { ProductoId = productoId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ingreso(IngresoViewModel vm)
    {
        var producto = await _db.Productos.FindAsync(vm.ProductoId);
        if (producto == null || !producto.Activo)
        {
            ModelState.AddModelError(nameof(vm.ProductoId), "Seleccione un producto activo.");
        }
        if (!ModelState.IsValid)
        {
            await CargarListas(vm.ProductoId);
            return View(vm);
        }

        producto!.Stock += vm.Cantidad;
        if (vm.CostoUnitario.HasValue && vm.CostoUnitario.Value > 0)
        {
            producto.CostoUnitario = vm.CostoUnitario.Value;
        }

        _db.Movimientos.Add(new MovimientoInventario
        {
            ProductoId = producto.Id,
            Tipo = TipoMovimiento.Ingreso,
            Cantidad = vm.Cantidad,
            StockResultante = producto.Stock,
            Motivo = string.IsNullOrWhiteSpace(vm.Observacion) ? "Ingreso de mercadería" : vm.Observacion,
            UsuarioId = UsuarioActualId(),
            ProveedorId = vm.ProveedorId
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Ingreso registrado: +{vm.Cantidad} de «{producto.Nombre}». Stock actual: {producto.Stock}.";
        return RedirectToAction(nameof(Index));
    }

    // RF16: ajuste de inventario
    public async Task<IActionResult> Ajuste(int? productoId)
    {
        await CargarListas(productoId);
        var vm = new AjusteViewModel { ProductoId = productoId ?? 0 };
        if (productoId.HasValue)
        {
            vm.NuevoStock = (await _db.Productos.FindAsync(productoId.Value))?.Stock ?? 0;
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajuste(AjusteViewModel vm)
    {
        var producto = await _db.Productos.FindAsync(vm.ProductoId);
        if (producto == null || !producto.Activo)
        {
            ModelState.AddModelError(nameof(vm.ProductoId), "Seleccione un producto activo.");
        }
        else if (producto.Stock == vm.NuevoStock)
        {
            ModelState.AddModelError(nameof(vm.NuevoStock), "El stock indicado es igual al stock actual; no hay nada que ajustar.");
        }
        if (!ModelState.IsValid)
        {
            await CargarListas(vm.ProductoId);
            return View(vm);
        }

        var diferencia = vm.NuevoStock - producto!.Stock;
        producto.Stock = vm.NuevoStock;

        _db.Movimientos.Add(new MovimientoInventario
        {
            ProductoId = producto.Id,
            Tipo = TipoMovimiento.Ajuste,
            Cantidad = diferencia,
            StockResultante = producto.Stock,
            Motivo = vm.Motivo,
            UsuarioId = UsuarioActualId()
        });
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Ajuste registrado para «{producto.Nombre}» ({(diferencia > 0 ? "+" : "")}{diferencia}). Stock actual: {producto.Stock}.";
        return RedirectToAction(nameof(Index));
    }

    // Historial de movimientos (kardex)
    public async Task<IActionResult> Movimientos(int? productoId)
    {
        var consulta = _db.Movimientos
            .AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (productoId.HasValue)
        {
            consulta = consulta.Where(m => m.ProductoId == productoId.Value);
        }

        ViewBag.Productos = new SelectList(
            await _db.Productos.OrderBy(p => p.Nombre).ToListAsync(), "Id", "Nombre", productoId);

        return View(await consulta.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Take(300).ToListAsync());
    }

    private async Task CargarListas(int? productoId)
    {
        ViewBag.Productos = new SelectList(
            await _db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(),
            "Id", "Nombre", productoId);
        ViewBag.Proveedores = new SelectList(
            await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(),
            "Id", "Nombre");
    }

    private int UsuarioActualId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
