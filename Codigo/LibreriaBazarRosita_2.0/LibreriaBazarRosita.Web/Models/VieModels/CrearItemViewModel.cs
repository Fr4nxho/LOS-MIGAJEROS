
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class CrearItemViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Ingrese el nombre.")]
        [StringLength(120, ErrorMessage = "El nombre es demasiado largo.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el tipo de ítem.")]
        [RegularExpression(
            "^(PRODUCTO|SERVICIO)$",
            ErrorMessage = "Seleccione un tipo válido.")]
        [Display(Name = "Tipo de ítem")]
        public string TipoItem { get; set; } = "PRODUCTO";

        [Range(
            typeof(decimal),
            "0",
            "99999999.99",
            ErrorMessage = "Ingrese un precio válido.")]
        [Display(Name = "Precio de venta")]
        public decimal Precio { get; set; }

        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "El stock inicial no puede ser negativo.")]
        [Display(Name = "Stock inicial")]
        public int? StockInicial { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "El stock mínimo no puede ser negativo.")]
        [Display(Name = "Stock mínimo")]
        public int? StockMinimo { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (TipoItem == "PRODUCTO")
            {
                if (!IdCategoria.HasValue || IdCategoria <= 0)
                {
                    yield return new ValidationResult(
                        "Seleccione una categoría.",
                        new[] { nameof(IdCategoria) }
                    );
                }

                if (!StockInicial.HasValue)
                {
                    yield return new ValidationResult(
                        "Ingrese el stock inicial.",
                        new[] { nameof(StockInicial) }
                    );
                }

                if (!StockMinimo.HasValue)
                {
                    yield return new ValidationResult(
                        "Ingrese el stock mínimo.",
                        new[] { nameof(StockMinimo) }
                    );
                }
            }
        }
    }
}
