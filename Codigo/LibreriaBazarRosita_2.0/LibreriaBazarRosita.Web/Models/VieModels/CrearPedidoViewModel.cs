
using System.ComponentModel.DataAnnotations;

namespace LibreriaBazarRosita.Web.Models.ViewModels;

public class CrearPedidoViewModel
{
    [Required(ErrorMessage = "Seleccione un proveedor.")]
    [Range(1, int.MaxValue,
        ErrorMessage = "Seleccione un proveedor válido.")]
    public int? IdProveedor { get; set; }

    public List<LineaPedidoViewModel> Detalles { get; set; }
        = new() { new LineaPedidoViewModel() };
}

public class LineaPedidoViewModel
{
    public int? IdProducto { get; set; }

    public int? Cantidad { get; set; }

    public decimal? CostoUnitario { get; set; }
}
