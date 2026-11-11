
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class ProveedorFormViewModel
{
    [Required(ErrorMessage = "Ingrese el nombre del proveedor.")]
    [StringLength(120)]
    [Display(Name = "Nombre o razón social")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    [StringLength(120)]
    [Display(Name = "Correo electrónico")]
    public string? Correo { get; set; }

    [StringLength(200)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }
}
