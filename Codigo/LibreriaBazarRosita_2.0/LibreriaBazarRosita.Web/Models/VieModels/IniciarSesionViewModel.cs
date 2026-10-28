
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels
{
    public class IniciarSesionViewModel
    {
        [Required(ErrorMessage = "Ingrese su usuario.")]
        [StringLength(50)]
        [Display(Name = "Nombre de usuario")]
        public string NombreUsuario { get; set; } = "";

        [Required(ErrorMessage = "Ingrese su contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; } = "";
    }
}
