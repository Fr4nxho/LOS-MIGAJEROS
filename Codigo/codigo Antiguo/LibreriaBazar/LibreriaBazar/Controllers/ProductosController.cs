using LibreriaBazar.Data;
using LibreriaBazar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class ProductosController : Controller
{
    private readonly AppDbContext _db;

    public ProductosController(AppDbContext db) => _db = db;

    // RF06 y RF07: búsqueda por nombre y por categoría.
    // RNF17: sin distinguir mayúsculas, minúsculas ni tildes (intercalación CI_AI).
    // RNF12: consulta asíncrona para no bloquear la interfaz.
    public async Task<IActionResult> Index(string? buscar, int? categoriaId, bool incluirInactivos = false)
    {
        var consulta = _db.Productos.Include(p => p.Categoria).AsNoTracking();

        if (!incluirInactivos)
        {
            consulta = consulta.Where(p => p.Activo);
        }
        if (categoriaId.HasValue)
        {
            consulta = consulta.Where(p => p.CategoriaId == categoriaId.Value);
        }
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(p =>
                EF.Functions.Collate(p.Nombre, "Latin1_General_CI_AI").Contains(termino));
        }

        ViewBag.Categorias = new SelectList(
            await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", categoriaId);
        ViewBag.Buscar = buscar;
        ViewBag.IncluirInactivos = incluirInactivos;

        return View(await consulta.OrderBy(p => p.Nombre).ToListAsync());
    }

    // RF03
    public async Task<IActionResult> Create()
    {
        await CargarCategorias(null);
        return View(new Producto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto modelo)
    {
        await ValidarCodigo(modelo);
        if (!ModelState.IsValid)
        {
            await CargarCategorias(modelo.CategoriaId);
            return View(modelo);
        }

        modelo.Id = 0;
        modelo.Activo = true;
        _db.Productos.Add(modelo);
        await _db.SaveChangesAsync();

        if (modelo.Stock > 0)
        {
            _db.Movimientos.Add(new MovimientoInventario
            {
                ProductoId = modelo.Id,
                Tipo = TipoMovimiento.Ingreso,
                Cantidad = modelo.Stock,
                StockResultante = modelo.Stock,
                Motivo = "Stock inicial al registrar el producto",
                UsuarioId = UsuarioActualId()
            });
            await _db.SaveChangesAsync();
        }

        TempData["Ok"] = $"Producto «{modelo.Nombre}» registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // RF04
    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        await CargarCategorias(producto.CategoriaId);
        return View(producto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Producto modelo)
    {
        if (id != modelo.Id) return NotFound();

        // El stock solo se modifica desde Inventario (ingresos, ventas y ajustes)
        ModelState.Remove(nameof(Producto.Stock));
        await ValidarCodigo(modelo);
        if (!ModelState.IsValid)
        {
            await CargarCategorias(modelo.CategoriaId);
            return View(modelo);
        }

        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        producto.Codigo = modelo.Codigo;
        producto.Nombre = modelo.Nombre;
        producto.Descripcion = modelo.Descripcion;
        producto.CategoriaId = modelo.CategoriaId;
        producto.PrecioVenta = modelo.PrecioVenta;
        producto.CostoUnitario = modelo.CostoUnitario;
        producto.StockMinimo = modelo.StockMinimo;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Producto «{producto.Nombre}» actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // RF05: desactivar (o reactivar) productos
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        producto.Activo = !producto.Activo;
        await _db.SaveChangesAsync();

        TempData["Ok"] = producto.Activo
            ? $"Producto «{producto.Nombre}» reactivado."
            : $"Producto «{producto.Nombre}» desactivado. Ya no aparecerá en las ventas.";
        return RedirectToAction(nameof(Index), new { incluirInactivos = !producto.Activo });
    }

    private async Task ValidarCodigo(Producto modelo)
    {
        if (!string.IsNullOrWhiteSpace(modelo.Codigo) &&
            await _db.Productos.AnyAsync(p => p.Codigo == modelo.Codigo && p.Id != modelo.Id))
        {
            ModelState.AddModelError(nameof(Producto.Codigo), "Ya existe un producto con ese código.");
        }
    }

    private async Task CargarCategorias(int? seleccionada)
    {
        ViewBag.Categorias = new SelectList(
            await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", seleccionada);
    }

    private int? UsuarioActualId()
    {
        var valor = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(valor, out var id) ? id : null;
    }
}
