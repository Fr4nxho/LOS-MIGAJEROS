
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class AjustarInventarioViewModel
    {
        public int IdProducto { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public int StockActual { get; set; }

        public int StockMinimo { get; set; }

        [Display(Name = "Cantidad física")]
        [Required(ErrorMessage = "Ingrese la cantidad física.")]
        [Range(0, int.MaxValue,
            ErrorMessage = "La cantidad no puede ser negativa.")]
        public int? CantidadFisica { get; set; }

        [Display(Name = "Motivo del ajuste")]
        [Required(ErrorMessage = "Ingrese el motivo del ajuste.")]
        [StringLength(200, MinimumLength = 5,
            ErrorMessage = "El motivo debe tener entre 5 y 200 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
