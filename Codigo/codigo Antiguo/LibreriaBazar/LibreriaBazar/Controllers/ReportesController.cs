using ClosedXML.Excel;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class ReportesController : Controller
{
    private const string ExcelMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly AppDbContext _db;

    public ReportesController(AppDbContext db) => _db = db;

    public IActionResult Index() => RedirectToAction(nameof(Ventas));

    // RF19: reporte de ventas
    public async Task<IActionResult> Ventas(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-6)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        var ventas = await ObtenerVentas(d, h);

        var vm = new ReporteVentasViewModel
        {
            Desde = d,
            Hasta = h,
            Ventas = ventas,
            PorDia = ventas
                .GroupBy(v => v.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DiaResumen { Dia = g.Key, Cantidad = g.Count(), Monto = g.Sum(v => v.Total) })
                .ToList()
        };
        return View(vm);
    }

    // RF26: exportar resumen de ventas a Excel
    public async Task<IActionResult> ExportarVentas(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-6)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        var ventas = await ObtenerVentas(d, h);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas");

        hoja.Cell(1, 1).Value = $"Reporte de ventas del {d:dd/MM/yyyy} al {h:dd/MM/yyyy}";
        hoja.Cell(1, 1).Style.Font.Bold = true;

        var encabezados = new[] { "N°", "Fecha", "Atendió", "Detalle", "Subtotal", "Descuento", "Total", "Método de pago" };
        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(3, i + 1).Value = encabezados[i];
            hoja.Cell(3, i + 1).Style.Font.Bold = true;
        }

        var fila = 4;
        foreach (var v in ventas)
        {
            hoja.Cell(fila, 1).Value = v.Id;
            hoja.Cell(fila, 2).Value = v.Fecha.ToString("dd/MM/yyyy HH:mm");
            hoja.Cell(fila, 3).Value = v.Usuario?.NombreCompleto ?? "";
            hoja.Cell(fila, 4).Value = string.Join(", ", v.Detalles.Select(x => $"{x.Cantidad} x {x.Descripcion}"));
            hoja.Cell(fila, 5).Value = v.Subtotal;
            hoja.Cell(fila, 6).Value = v.Descuento;
            hoja.Cell(fila, 7).Value = v.Total;
            hoja.Cell(fila, 8).Value = v.Pago?.Metodo ?? "";
            fila++;
        }

        hoja.Cell(fila + 1, 4).Value = "TOTAL INGRESOS";
        hoja.Cell(fila + 1, 4).Style.Font.Bold = true;
        hoja.Cell(fila + 1, 7).Value = ventas.Sum(v => v.Total);
        hoja.Cell(fila + 1, 7).Style.Font.Bold = true;

        hoja.Range(4, 5, fila + 1, 7).Style.NumberFormat.Format = "#,##0.00";
        hoja.Columns().AdjustToContents();

        return GenerarExcel(libro, $"ventas_{d:yyyyMMdd}_{h:yyyyMMdd}.xlsx");
    }

    // RF20: reporte de inventario
    public async Task<IActionResult> Inventario()
    {
        var vm = new ReporteInventarioViewModel { Productos = await ObtenerProductos() };
        return View(vm);
    }

    // RF25: exportar inventario a Excel
    public async Task<IActionResult> ExportarInventario()
    {
        var productos = await ObtenerProductos();

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Inventario");

        hoja.Cell(1, 1).Value = $"Reporte de inventario al {DateTime.Now:dd/MM/yyyy HH:mm}";
        hoja.Cell(1, 1).Style.Font.Bold = true;

        var encabezados = new[] { "Código", "Producto", "Categoría", "Stock", "Stock mínimo", "Costo unitario", "Precio venta", "Valor en stock", "Estado" };
        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(3, i + 1).Value = encabezados[i];
            hoja.Cell(3, i + 1).Style.Font.Bold = true;
        }

        var fila = 4;
        foreach (var p in productos)
        {
            hoja.Cell(fila, 1).Value = p.Codigo ?? "";
            hoja.Cell(fila, 2).Value = p.Nombre;
            hoja.Cell(fila, 3).Value = p.Categoria?.Nombre ?? "";
            hoja.Cell(fila, 4).Value = p.Stock;
            hoja.Cell(fila, 5).Value = p.StockMinimo;
            hoja.Cell(fila, 6).Value = p.CostoUnitario;
            hoja.Cell(fila, 7).Value = p.PrecioVenta;
            hoja.Cell(fila, 8).Value = p.Stock * p.CostoUnitario;
            hoja.Cell(fila, 9).Value = p.StockBajo ? "STOCK BAJO" : "Normal";
            fila++;
        }

        hoja.Cell(fila + 1, 7).Value = "VALOR TOTAL";
        hoja.Cell(fila + 1, 7).Style.Font.Bold = true;
        hoja.Cell(fila + 1, 8).Value = productos.Sum(p => p.Stock * p.CostoUnitario);
        hoja.Cell(fila + 1, 8).Style.Font.Bold = true;

        hoja.Range(4, 6, fila + 1, 8).Style.NumberFormat.Format = "#,##0.00";
        hoja.Columns().AdjustToContents();

        return GenerarExcel(libro, $"inventario_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    private async Task<List<Venta>> ObtenerVentas(DateTime desde, DateTime hasta)
    {
        return await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Pago)
            .Include(v => v.Detalles)
            .Where(v => v.Fecha >= desde && v.Fecha < hasta.AddDays(1))
            .OrderBy(v => v.Fecha)
            .ToListAsync();
    }

    private async Task<List<Producto>> ObtenerProductos()
    {
        return await _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
    }

    private FileContentResult GenerarExcel(XLWorkbook libro, string nombreArchivo)
    {
        using var ms = new MemoryStream();
        libro.SaveAs(ms);
        return File(ms.ToArray(), ExcelMime, nombreArchivo);
    }
}
