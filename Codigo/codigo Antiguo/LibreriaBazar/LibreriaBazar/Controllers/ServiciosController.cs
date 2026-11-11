using LibreriaBazar.Data;
using LibreriaBazar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class ServiciosController : Controller
{
    private readonly AppDbContext _db;

    public ServiciosController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(bool incluirInactivos = false)
    {
        var consulta = _db.Servicios.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(s => s.Activo);
        }
        ViewBag.IncluirInactivos = incluirInactivos;
        return View(await consulta.OrderBy(s => s.Nombre).ToListAsync());
    }

    public IActionResult Create() => View(new Servicio());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Servicio modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        modelo.Id = 0;
        modelo.Activo = true;
        _db.Servicios.Add(modelo);
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Servicio «{modelo.Nombre}» registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio == null) return NotFound();
        return View(servicio);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Servicio modelo)
    {
        if (id != modelo.Id) return NotFound();
        if (!ModelState.IsValid) return View(modelo);

        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio == null) return NotFound();

        servicio.Nombre = modelo.Nombre;
        servicio.Descripcion = modelo.Descripcion;
        servicio.Precio = modelo.Precio;
        await _db.SaveChangesAsync();

        TempData["Ok"] = $"Servicio «{servicio.Nombre}» actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var servicio = await _db.Servicios.FindAsync(id);
        if (servicio == null) return NotFound();

        servicio.Activo = !servicio.Activo;
        await _db.SaveChangesAsync();

        TempData["Ok"] = servicio.Activo
            ? $"Servicio «{servicio.Nombre}» reactivado."
            : $"Servicio «{servicio.Nombre}» desactivado.";
        return RedirectToAction(nameof(Index), new { incluirInactivos = !servicio.Activo });
    }
}
