namespace LibreriaBazar.Models;

public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public List<DetalleVenta> Detalles { get; set; } = new();
    public PagoCliente? Pago { get; set; }
}

/// <summary>Una línea de la venta: un producto o un servicio (RF08).</summary>
public class DetalleVenta
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public int? ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int? ServicioId { get; set; }
    public Servicio? Servicio { get; set; }

    // Se guarda el nombre y el precio vigentes al momento de la venta
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Pago realizado por el cliente (RF12).</summary>
public class PagoCliente
{
    public int Id { get; set; }

    public int VentaId { get; set; }
    public Venta? Venta { get; set; }

    public string Metodo { get; set; } = "Efectivo";
    public decimal MontoRecibido { get; set; }
    public decimal Vuelto { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}
