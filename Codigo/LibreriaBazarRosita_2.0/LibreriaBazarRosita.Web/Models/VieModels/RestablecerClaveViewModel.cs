
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class RestablecerClaveViewModel
{
    public int IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [StringLength(
        100,
        MinimumLength = 10,
        ErrorMessage = "La contraseña debe tener entre 10 y 100 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaClave { get; set; } = "";

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(NuevaClave),
        ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarClave { get; set; } = "";
}
