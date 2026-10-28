namespace LibreriaBazar.Services.Pagos;

/// <summary>Cobro en efectivo: el cliente entrega un monto y se calcula el vuelto (RF12).</summary>
public class PagoEfectivo : IEstrategiaPago
{
    public string Nombre => "Efectivo";
    public bool RequiereMontoRecibido => true;

    public ResultadoPago Procesar(decimal total, decimal montoRecibido)
    {
        if (montoRecibido < total)
            return ResultadoPago.Falla("El monto recibido es menor al total de la venta.");

        return ResultadoPago.Ok(montoRecibido, montoRecibido - total);
    }
}

/// <summary>Pago digital por Yape o Plin: se cobra el monto exacto, no hay vuelto.</summary>
public class PagoYapePlin : IEstrategiaPago
{
    public string Nombre => "Yape / Plin";
    public bool RequiereMontoRecibido => false;

    public ResultadoPago Procesar(decimal total, decimal montoRecibido) =>
        ResultadoPago.Ok(total, 0);
}

/// <summary>Pago con tarjeta (POS): se cobra el monto exacto, no hay vuelto.</summary>
public class PagoTarjeta : IEstrategiaPago
{
    public string Nombre => "Tarjeta";
    public bool RequiereMontoRecibido => false;

    public ResultadoPago Procesar(decimal total, decimal montoRecibido) =>
        ResultadoPago.Ok(total, 0);
}
