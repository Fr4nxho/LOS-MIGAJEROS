
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class EditarItemViewModel
    {
        // Identificador del ítem que se modificará.
        [Required]
        public int IdItem { get; set; }

        // Solo para mostrar el tipo en el formulario.
        // El servidor lo verificará desde la base de datos.
        public string TipoItem { get; set; } = "";

        // Datos generales.
        [Required(ErrorMessage = "Ingrese el nombre.")]
        [StringLength(
            120,
            ErrorMessage = "El nombre no puede superar 120 caracteres."
        )]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        [Range(
            typeof(decimal),
            "0",
            "99999999.99",
            ErrorMessage = "Ingrese un precio válido."
        )]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        // Datos exclusivos de productos.
        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        [Range(
            0,
            int.MaxValue,
            ErrorMessage = "El stock mínimo no puede ser negativo."
        )]
        [Display(Name = "Stock mínimo")]
        public int? StockMinimo { get; set; }
    }
}
