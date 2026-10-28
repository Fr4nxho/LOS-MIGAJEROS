using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string NombreUsuario { get; set; } = null!;

    public string ClaveHash { get; set; } = null!;

    public string Rol { get; set; } = null!;

    public bool Estado { get; set; }

    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    public virtual ICollection<PedidoProveedor> PedidoProveedors { get; set; } = new List<PedidoProveedor>();

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
