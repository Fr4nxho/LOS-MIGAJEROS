
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class DetalleRegistrarVentaViewModel
    {
        // Producto o servicio seleccionado.
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Seleccione un producto o servicio.")]
        public int IdItem { get; set; }

        // Cantidad que desea comprar el cliente.
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "La cantidad debe ser mayor que cero.")]
        public int Cantidad { get; set; } = 1;
    }
}
