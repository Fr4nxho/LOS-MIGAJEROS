using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class ItemCatalogo
{
    public int IdItem { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal Precio { get; set; }

    public string TipoItem { get; set; } = null!;

    public bool Estado { get; set; }

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Producto? Producto { get; set; }

    public virtual Servicio? Servicio { get; set; }
}
