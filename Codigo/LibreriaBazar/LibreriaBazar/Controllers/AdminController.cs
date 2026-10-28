using LibreriaBazar.Data;
using LibreriaBazar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

// RNF09: copias de respaldo de la información. Solo el propietario.
[Authorize(Roles = Roles.Propietario)]
public class AdminController : Controller
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db) => _db = db;

    public IActionResult Respaldo()
    {
        ViewBag.Carpeta = CarpetaPorDefecto();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Respaldo(string? carpeta)
    {
        carpeta = string.IsNullOrWhiteSpace(carpeta) ? CarpetaPorDefecto() : carpeta.Trim();

        try
        {
            var nombreBd = _db.Database.GetDbConnection().Database;
            Directory.CreateDirectory(carpeta);
            var archivo = Path.Combine(carpeta, $"{nombreBd}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

            await _db.Database.ExecuteSqlRawAsync(
                $"BACKUP DATABASE [{nombreBd}] TO DISK = {{0}} WITH INIT", archivo);

            TempData["Ok"] = $"Copia de respaldo generada correctamente en: {archivo}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "No se pudo generar la copia de respaldo. Verifique que la carpeta exista y que SQL Server tenga permiso de escritura. Detalle: "
                                + ex.Message;
        }

        ViewBag.Carpeta = carpeta;
        return View();
    }

    private static string CarpetaPorDefecto() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RespaldosLibreriaBazar");
}
