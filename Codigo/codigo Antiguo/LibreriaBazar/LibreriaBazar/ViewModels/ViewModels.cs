using System.ComponentModel.DataAnnotations;
using LibreriaBazar.Models;

namespace LibreriaBazar.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su usuario.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class DashboardViewModel
{
    public int VentasHoy { get; set; }
    public decimal IngresosHoy { get; set; }
    public int TotalProductos { get; set; }
    public List<Producto> BajoStock { get; set; } = new();
    public List<Venta> UltimasVentas { get; set; } = new();
}

// ----- Ventas -----

public class CatalogoItemVm
{
    public string Tipo { get; set; } = "P"; // P = producto, S = servicio
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int? Stock { get; set; }
}

public class LineaVentaVm
{
    public string Tipo { get; set; } = "P";
    public int ItemId { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class VentaCreateViewModel
{
    public List<LineaVentaVm> Lineas { get; set; } = new();

    [Range(0, 1000000, ErrorMessage = "Ingrese un descuento válido (0 o mayor).")]
    public decimal Descuento { get; set; }

    [Required]
    public string MetodoPago { get; set; } = "Efectivo";

    [Range(0, 1000000, ErrorMessage = "Ingrese un monto recibido válido.")]
    public decimal MontoRecibido { get; set; }

    public List<CatalogoItemVm> Catalogo { get; set; } = new();
}

// ----- Inventario -----

public class IngresoViewModel
{
    [Display(Name = "Producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto.")]
    public int ProductoId { get; set; }

    [Range(1, 1000000, ErrorMessage = "La cantidad debe ser un entero mayor que 0.")]
    public int Cantidad { get; set; } = 1;

    [Display(Name = "Costo unitario (opcional)")]
    [Range(0, 100000, ErrorMessage = "Ingrese un costo válido.")]
    public decimal? CostoUnitario { get; set; }

    [Display(Name = "Proveedor (opcional)")]
    public int? ProveedorId { get; set; }

    [StringLength(200)]
    public string? Observacion { get; set; }
}

public class AjusteViewModel
{
    [Display(Name = "Producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un producto.")]
    public int ProductoId { get; set; }

    [Display(Name = "Stock real contado")]
    [Range(0, 1000000, ErrorMessage = "El stock debe ser un entero válido (0 o mayor).")]
    public int NuevoStock { get; set; }

    [Required(ErrorMessage = "Indique el motivo del ajuste."), StringLength(200)]
    public string Motivo { get; set; } = string.Empty;
}

// ----- Pedidos a proveedores -----

public class LineaPedidoVm
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal CostoUnitario { get; set; }
}

public class PedidoCreateViewModel
{
    [Display(Name = "Proveedor")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un proveedor.")]
    public int ProveedorId { get; set; }

    [StringLength(200)]
    public string? Observacion { get; set; }

    public List<LineaPedidoVm> Lineas { get; set; } = new();

    public List<Producto> Productos { get; set; } = new();
}

public class PagoProveedorViewModel
{
    public int PedidoId { get; set; }

    [Range(0.01, 1000000, ErrorMessage = "Ingrese un monto válido (mayor que 0).")]
    public decimal Monto { get; set; }

    [Required]
    public string Metodo { get; set; } = "Efectivo";

    [StringLength(200)]
    public string? Observacion { get; set; }
}

// ----- Reportes -----

public class DiaResumen
{
    public DateTime Dia { get; set; }
    public int Cantidad { get; set; }
    public decimal Monto { get; set; }
}

public class ReporteVentasViewModel
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public List<Venta> Ventas { get; set; } = new();
    public List<DiaResumen> PorDia { get; set; } = new();
    public int CantidadVentas => Ventas.Count;
    public decimal Subtotal => Ventas.Sum(v => v.Subtotal);
    public decimal Descuentos => Ventas.Sum(v => v.Descuento);
    public decimal Ingresos => Ventas.Sum(v => v.Total);
}

public class ReporteInventarioViewModel
{
    public List<Producto> Productos { get; set; } = new();
    public decimal ValorInventario => Productos.Sum(p => p.Stock * p.CostoUnitario);
    public int CantidadBajoStock => Productos.Count(p => p.StockBajo);
}
