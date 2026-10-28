using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class MovimientoInventario
{
    public int IdMovimiento { get; set; }

    public int IdProducto { get; set; }

    public int IdUsuario { get; set; }

    public int? IdVenta { get; set; }

    public int? IdPedido { get; set; }

    public DateTime FechaHora { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public int Cantidad { get; set; }

    public string? Motivo { get; set; }

    public virtual PedidoProveedor? IdPedidoNavigation { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual Ventum? IdVentaNavigation { get; set; }
}
