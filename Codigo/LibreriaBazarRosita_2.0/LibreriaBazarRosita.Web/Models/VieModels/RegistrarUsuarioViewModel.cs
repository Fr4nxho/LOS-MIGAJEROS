
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class RegistrarUsuarioViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre completo.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingrese el nombre de usuario.")]
    [StringLength(50, MinimumLength = 4)]
    [RegularExpression(
        @"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Utilice letras, números, puntos, guiones o guiones bajos.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = "";

    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [StringLength(100, MinimumLength = 10,
        ErrorMessage = "La contraseña debe tener al menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Clave { get; set; } = "";

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Clave),
        ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarClave { get; set; } = "";
}
