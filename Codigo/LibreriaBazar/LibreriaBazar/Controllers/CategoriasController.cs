using LibreriaBazar.Data;
using LibreriaBazar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class CategoriasController : Controller
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var categorias = await _db.Categorias
            .Include(c => c.Productos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return View(categorias);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(string? nombre)
    {
        nombre = nombre?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            TempData["Error"] = "Ingrese el nombre de la categoría.";
            return RedirectToAction(nameof(Index));
        }
        if (nombre.Length > 60)
        {
            TempData["Error"] = "El nombre de la categoría no puede superar los 60 caracteres.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Categorias.AnyAsync(c => c.Nombre == nombre))
        {
            TempData["Error"] = "Ya existe una categoría con ese nombre.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categorias.Add(new Categoria { Nombre = nombre });
        await _db.SaveChangesAsync();
        TempData["Ok"] = $"Categoría «{nombre}» registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
