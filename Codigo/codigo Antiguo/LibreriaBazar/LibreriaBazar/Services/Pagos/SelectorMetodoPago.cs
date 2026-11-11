namespace LibreriaBazar.Services.Pagos;

/// <summary>
/// Contexto del patrón Strategy: recibe todas las estrategias registradas en Program.cs
/// (inyección de dependencias) y entrega la que corresponde al método elegido en la pantalla.
/// Agregar un método de pago nuevo = crear una clase IEstrategiaPago y registrarla; nada más cambia.
/// </summary>
public class SelectorMetodoPago
{
    private readonly Dictionary<string, IEstrategiaPago> _estrategias;

    public SelectorMetodoPago(IEnumerable<IEstrategiaPago> estrategias) =>
        _estrategias = estrategias.ToDictionary(e => e.Nombre, StringComparer.OrdinalIgnoreCase);

    /// <summary>Nombres para llenar la lista desplegable de la pantalla de ventas.</summary>
    public IReadOnlyList<string> Nombres => _estrategias.Values.Select(e => e.Nombre).ToList();

    /// <summary>Métodos que piden «monto recibido» (la vista lo usa para mostrar u ocultar ese campo).</summary>
    public IReadOnlyList<string> NombresConMontoRecibido =>
        _estrategias.Values.Where(e => e.RequiereMontoRecibido).Select(e => e.Nombre).ToList();

    /// <summary>Devuelve la estrategia del método indicado, o null si no existe.</summary>
    public IEstrategiaPago? Obtener(string? nombre) =>
        nombre != null && _estrategias.TryGetValue(nombre, out var e) ? e : null;
}
