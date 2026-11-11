
namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class ResumenDiarioViewModel
{
    public DateTime Fecha { get; set; }

    public int CantidadVentas { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Descuentos { get; set; }

    public decimal Ingresos { get; set; }

    public List<ResumenMedioPagoViewModel> MediosPago { get; set; }
        = new();
}

public class ResumenMedioPagoViewModel
{
    public string MedioPago { get; set; } = "";

    public int Cantidad { get; set; }

    public decimal Total { get; set; }
}
