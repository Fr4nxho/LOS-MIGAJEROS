
namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class ReporteVentasViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    public List<VentaReporteFilaViewModel> Ventas { get; set; }
        = new();

    public int CantidadVentas => Ventas.Count;

    public decimal Subtotal =>
        Ventas.Sum(v => v.Subtotal);

    public decimal Descuentos =>
        Ventas.Sum(v => v.Descuento);

    public decimal Ingresos =>
        Ventas.Sum(v => v.Total);

    public List<VentasPorDiaViewModel> PorDia =>
        Ventas
            .GroupBy(v => v.FechaHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new VentasPorDiaViewModel
            {
                Fecha = g.Key,
                Cantidad = g.Count(),
                Subtotal = g.Sum(v => v.Subtotal),
                Descuentos = g.Sum(v => v.Descuento),
                Ingresos = g.Sum(v => v.Total)
            })
            .ToList();
}

public class VentaReporteFilaViewModel
{
    public int IdVenta { get; set; }
    public DateTime FechaHora { get; set; }
    public string Usuario { get; set; } = "";
    public string MedioPago { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
}

public class VentasPorDiaViewModel
{
    public DateTime Fecha { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Ingresos { get; set; }
}
