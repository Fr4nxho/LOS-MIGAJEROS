using System.ComponentModel.DataAnnotations;

namespace LibreriaBazar.Models;

public class Producto
{
    public int Id { get; set; }

    [StringLength(30)]
    [Display(Name = "Código")]
    public string? Codigo { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio."), StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(250)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    [Display(Name = "Precio de venta")]
    [Range(0.01, 100000, ErrorMessage = "Ingrese un precio de venta válido (mayor que 0).")]
    public decimal PrecioVenta { get; set; }

    [Display(Name = "Costo unitario")]
    [Range(0, 100000, ErrorMessage = "Ingrese un costo válido (0 o mayor).")]
    public decimal CostoUnitario { get; set; }

    [Range(0, 1000000, ErrorMessage = "El stock debe ser un número entero válido.")]
    public int Stock { get; set; }

    [Display(Name = "Stock mínimo")]
    [Range(0, 1000000, ErrorMessage = "El stock mínimo debe ser un número entero válido.")]
    public int StockMinimo { get; set; } = 5;

    public bool Activo { get; set; } = true;

    public bool StockBajo => Stock <= StockMinimo;
}
