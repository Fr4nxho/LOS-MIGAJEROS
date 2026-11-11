
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class RegistrarPagoProveedorViewModel
{
    [Range(1, int.MaxValue)]
    public int IdPedido { get; set; }

    [Required(ErrorMessage = "Seleccione un medio de pago.")]
    [Display(Name = "Medio de pago")]
    public string MedioPago { get; set; } = "";

    // Datos utilizados únicamente para mostrar el formulario.
    public string NombreProveedor { get; set; } = "";
    public decimal TotalPedido { get; set; }
}
