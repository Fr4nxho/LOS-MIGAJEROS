using System.Diagnostics;
using LibreriaBazar.Data;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    // Pantalla principal: ingresos del día (RF18), ventas del día (RF24) y alertas de stock bajo (RF23)
    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        var ventasHoy = await _db.Ventas
            .Where(v => v.Fecha >= hoy && v.Fecha < manana)
            .Select(v => v.Total)
            .ToListAsync();

        var vm = new DashboardViewModel
        {
            VentasHoy = ventasHoy.Count,
            IngresosHoy = ventasHoy.Sum(),
            TotalProductos = await _db.Productos.CountAsync(p => p.Activo),
            BajoStock = await _db.Productos
                .Where(p => p.Activo && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToListAsync(),
            UltimasVentas = await _db.Ventas
                .Include(v => v.Usuario)
                .Where(v => v.Fecha >= hoy && v.Fecha < manana)
                .OrderByDescending(v => v.Fecha)
                .Take(8)
                .ToListAsync()
        };

        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }
}
