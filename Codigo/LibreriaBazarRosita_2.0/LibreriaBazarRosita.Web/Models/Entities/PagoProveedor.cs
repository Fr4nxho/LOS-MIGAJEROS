using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class PagoProveedor
{
    public int IdPedido { get; set; }

    public DateTime FechaHora { get; set; }

    public decimal Monto { get; set; }

    public string MedioPago { get; set; } = null!;

    public virtual PedidoProveedor IdPedidoNavigation { get; set; } = null!;
}
