using LibreriaBazar.Data;
using LibreriaBazar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

// RF17: registrar y actualizar proveedores
public class ProveedoresController : Controller
{
    private readonly AppDbContext _db;

    public ProveedoresController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(bool incluirInactivos = false)
    {
        var consulta = _db.Proveedores.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(p => p.Activo);
        }
        ViewBag.IncluirInactivos = incluirInactivos;
        return View(await consulta.OrderBy(p => p.Nombre).ToListAsync());
    }

    public IActionResult Create() => View(new Proveedor());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Proveedor modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        modelo.Id = 0;
        modelo.Activo = true;
        _db.Proveedores.Add(modelo);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Proveedor «{modelo.Nombre}» registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();
        return View(proveedor);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Proveedor modelo)
    {
        if (id != modelo.Id) return NotFound();
        if (!ModelState.IsValid) return View(modelo);

        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        proveedor.Nombre = modelo.Nombre;
        proveedor.Ruc = modelo.Ruc;
        proveedor.Telefono = modelo.Telefono;
        proveedor.Correo = modelo.Correo;
        proveedor.Direccion = modelo.Direccion;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Proveedor «{proveedor.Nombre}» actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor == null) return NotFound();

        proveedor.Activo = !proveedor.Activo;
        await _db.SaveChangesAsync();

        TempData["Ok"] = proveedor.Activo
            ? $"Proveedor «{proveedor.Nombre}» reactivado."
            : $"Proveedor «{proveedor.Nombre}» desactivado.";
        return RedirectToAction(nameof(Index), new { incluirInactivos = !proveedor.Activo });
    }
}
