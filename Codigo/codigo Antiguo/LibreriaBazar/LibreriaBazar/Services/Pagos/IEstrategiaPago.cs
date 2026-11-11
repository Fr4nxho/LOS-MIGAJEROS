namespace LibreriaBazar.Services.Pagos;

/// <summary>
/// Patrón STRATEGY (GoF – comportamiento).
/// Cada método de pago aceptado en caja (efectivo, Yape/Plin, tarjeta...) encapsula su propia regla
/// para validar el cobro y calcular el vuelto. VentasController solo conoce esta interfaz.
/// </summary>
public interface IEstrategiaPago
{
    /// <summary>Nombre que se muestra al usuario y se guarda en PagoCliente.Metodo.</summary>
    string Nombre { get; }

    /// <summary>Indica si en pantalla se debe pedir el «monto recibido» (solo el efectivo lo necesita).</summary>
    bool RequiereMontoRecibido { get; }

    /// <summary>Valida el cobro y calcula el monto recibido y el vuelto según la regla del método.</summary>
    ResultadoPago Procesar(decimal total, decimal montoRecibido);
}

/// <summary>Resultado de aplicar una estrategia de pago a una venta.</summary>
public record ResultadoPago(bool Exitoso, decimal MontoRecibido, decimal Vuelto, string? Error = null)
{
    public static ResultadoPago Ok(decimal montoRecibido, decimal vuelto) => new(true, montoRecibido, vuelto);
    public static ResultadoPago Falla(string error) => new(false, 0, 0, error);
}
