using System;
using System.Collections.Generic;

namespace LibreriaBazarRosita.Web.Models.Entities;

public partial class Servicio
{
    public int IdServicio { get; set; }

    public virtual ItemCatalogo IdServicioNavigation { get; set; } = null!;
}
