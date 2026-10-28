using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class PedidoProveedor
{
    public int IdPedido { get; set; }

    public int IdProveedor { get; set; }

    public int IdUsuario { get; set; }

    public DateTime Fecha { get; set; }

    public string Estado { get; set; } = null!;

    public decimal Total { get; set; }

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    public virtual PagoProveedor? PagoProveedor { get; set; }
}
