using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class PagoVentum
{
    public int IdVenta { get; set; }

    public DateTime FechaHora { get; set; }

    public decimal Monto { get; set; }

    public string MedioPago { get; set; } = null!;

    public virtual Ventum IdVentaNavigation { get; set; } = null!;
}
