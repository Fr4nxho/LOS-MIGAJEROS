namespace LibreriaBazar.Models;

public enum TipoMovimiento
{
    Venta,
    Ingreso,
    Ajuste,
    RecepcionPedido
}

/// <summary>Kardex: cada cambio de stock queda registrado (RF13, RF15, RF16).</summary>
public class MovimientoInventario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Variación del stock: positivo si entra mercadería, negativo si sale.</summary>
    public int Cantidad { get; set; }
    public int StockResultante { get; set; }

    public string? Motivo { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
}
