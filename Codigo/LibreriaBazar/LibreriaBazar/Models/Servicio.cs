using System.ComponentModel.DataAnnotations;

namespace LibreriaBazar.Models;

/// <summary>Servicios de la librería: impresiones, fotocopias, anillados, etc.</summary>
public class Servicio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del servicio es obligatorio."), StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Ingrese un precio válido (mayor que 0).")]
    public decimal Precio { get; set; }

    public bool Activo { get; set; } = true;
}
