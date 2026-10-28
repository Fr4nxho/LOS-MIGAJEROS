namespace LibreriaBazar.Models;

public enum EstadoPedido
{
    Pendiente,
    Recibido,
    Cancelado
}

/// <summary>Pedido de mercadería a un proveedor (RF21).</summary>
public class PedidoProveedor
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;
    public decimal Total { get; set; }
    public string? Observacion { get; set; }

    public List<DetallePedido> Detalles { get; set; } = new();
    public List<PagoProveedor> Pagos { get; set; } = new();

    public decimal TotalPagado => Pagos.Sum(p => p.Monto);
    public decimal Saldo => Total - TotalPagado;
}

public class DetallePedido
{
    public int Id { get; set; }

    public int PedidoProveedorId { get; set; }
    public PedidoProveedor? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

/// <summary>Pago realizado a un proveedor (RF22).</summary>
public class PagoProveedor
{
    public int Id { get; set; }

    public int PedidoProveedorId { get; set; }
    public PedidoProveedor? Pedido { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Monto { get; set; }
    public string Metodo { get; set; } = "Efectivo";
    public string? Observacion { get; set; }
}
