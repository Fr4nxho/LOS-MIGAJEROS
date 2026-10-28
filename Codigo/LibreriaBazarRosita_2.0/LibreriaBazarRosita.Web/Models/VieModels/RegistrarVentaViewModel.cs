
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class RegistrarVentaViewModel
    {
        // Descuento aplicado al total de la venta.
        [Range(
            typeof(decimal),
            "0",
            "9999999999.99",
            ErrorMessage = "El descuento no puede ser negativo.")]
        public decimal Descuento { get; set; } = 0m;

        // Medio de pago del cliente.
        [Required(ErrorMessage = "Seleccione un medio de pago.")]
        public string MedioPago { get; set; } = "EFECTIVO";

        // Productos y/o servicios incluidos en la venta.
        [MinLength(
            1,
            ErrorMessage = "Debe agregar al menos un ítem.")]
        public List<DetalleRegistrarVentaViewModel> Detalles
        { get; set; } = new()
            {
                new DetalleRegistrarVentaViewModel()
            };
    }
}
