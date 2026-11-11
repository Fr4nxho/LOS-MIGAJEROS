using System.ComponentModel.DataAnnotations;

namespace LibreriaBazar.Models;

public class Proveedor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del proveedor es obligatorio."), StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(11)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener 11 dígitos.")]
    public string? Ruc { get; set; }

    [StringLength(20)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(100)]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    public string? Correo { get; set; }

    [StringLength(200)]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    public bool Activo { get; set; } = true;
}
