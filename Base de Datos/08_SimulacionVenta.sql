
USE [LibreriaBazarRosita];
GO

SET XACT_ABORT ON;
GO

-- =========================================
-- SIMULACION DE VENTA MIXTA
-- 2 cuadernos + 3 impresiones
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdUsuario INT;
    DECLARE @IdVenta INT;
    DECLARE @IdCuaderno INT;
    DECLARE @IdImpresion INT;

    DECLARE @PrecioCuaderno DECIMAL(12,2);
    DECLARE @PrecioImpresion DECIMAL(12,2);
    DECLARE @Subtotal DECIMAL(12,2);
    DECLARE @Total DECIMAL(12,2);

    -- =====================================
    -- 1. BUSCAR PRODUCTO Y SERVICIO
    -- =====================================

    SELECT TOP (1)
        @IdCuaderno = i.IdItem,
        @PrecioCuaderno = i.Precio
    FROM dbo.ItemCatalogo i
    INNER JOIN dbo.Producto p
        ON i.IdItem = p.IdProducto
    WHERE i.Nombre = N'Cuaderno A4'
      AND i.TipoItem = N'PRODUCTO'
      AND i.Estado = 1
    ORDER BY i.IdItem;

    SELECT TOP (1)
        @IdImpresion = i.IdItem,
        @PrecioImpresion = i.Precio
    FROM dbo.ItemCatalogo i
    INNER JOIN dbo.Servicio s
        ON i.IdItem = s.IdServicio
    WHERE i.Nombre = N'Impresion'
      AND i.TipoItem = N'SERVICIO'
      AND i.Estado = 1
    ORDER BY i.IdItem;

    IF @IdCuaderno IS NULL
       OR @IdImpresion IS NULL
    BEGIN
        THROW 50001,
            'Falta el cuaderno o el servicio de impresion.',
            1;
    END;

    -- =====================================
    -- 2. USUARIO TEMPORAL DE SIMULACION
    -- =====================================

    INSERT INTO dbo.Usuario
        (Nombre, NombreUsuario, ClaveHash, Rol, Estado)
    VALUES (
        N'Operador de simulacion',
        N'simulacion_' + CONVERT(NVARCHAR(36), NEWID()),
        N'NO_ES_UN_HASH_VALIDO_SOLO_PRUEBA',
        N'ENCARGADO',
        1
    );

    SET @IdUsuario = SCOPE_IDENTITY();

    -- =====================================
    -- 3. CALCULAR TOTAL
    -- =====================================

    SET @Subtotal =
        (2 * @PrecioCuaderno)
        + (3 * @PrecioImpresion);

    SET @Total = @Subtotal;

    -- =====================================
    -- 4. REGISTRAR VENTA
    -- =====================================

    INSERT INTO dbo.Venta
        (IdUsuario, Subtotal, Descuento, Total, Estado)
    VALUES (
        @IdUsuario,
        @Subtotal,
        0,
        @Total,
        N'COMPLETADA'
    );

    SET @IdVenta = SCOPE_IDENTITY();

    -- =====================================
    -- 5. REGISTRAR DETALLES
    -- =====================================

    INSERT INTO dbo.DetalleVenta
        (IdVenta, IdItem, Cantidad, PrecioUnitario, Subtotal)
    VALUES
    (
        @IdVenta,
        @IdCuaderno,
        2,
        @PrecioCuaderno,
        2 * @PrecioCuaderno
    ),
    (
        @IdVenta,
        @IdImpresion,
        3,
        @PrecioImpresion,
        3 * @PrecioImpresion
    );

    -- =====================================
    -- 6. REGISTRAR PAGO
    -- =====================================

    INSERT INTO dbo.PagoVenta
        (IdVenta, Monto, MedioPago)
    VALUES (
        @IdVenta,
        @Total,
        N'EFECTIVO'
    );

    -- =====================================
    -- 7. DESCONTAR STOCK DEL PRODUCTO
    -- =====================================

    UPDATE dbo.Producto
    SET StockActual = StockActual - 2
    WHERE IdProducto = @IdCuaderno
      AND StockActual >= 2;

    IF @@ROWCOUNT <> 1
    BEGIN
        THROW 50002,
            'Stock insuficiente para la venta.',
            1;
    END;

    -- =====================================
    -- 8. REGISTRAR SALIDA DE INVENTARIO
    -- =====================================

    INSERT INTO dbo.MovimientoInventario
        (
            IdProducto,
            IdUsuario,
            IdVenta,
            TipoMovimiento,
            Cantidad,
            Motivo
        )
    VALUES (
        @IdCuaderno,
        @IdUsuario,
        @IdVenta,
        N'SALIDA',
        -2,
        N'Venta de dos cuadernos A4'
    );

    -- =====================================
    -- 9. VERIFICAR DETALLES DE LA VENTA
    -- =====================================

    SELECT
        i.Nombre,
        i.TipoItem,
        d.Cantidad,
        d.PrecioUnitario,
        d.Subtotal,
        p.StockActual
    FROM dbo.DetalleVenta d
    INNER JOIN dbo.ItemCatalogo i
        ON d.IdItem = i.IdItem
    LEFT JOIN dbo.Producto p
        ON i.IdItem = p.IdProducto
    WHERE d.IdVenta = @IdVenta
    ORDER BY d.IdDetalleVenta;

    -- =====================================
    -- 10. VERIFICAR VENTA, PAGO Y MOVIMIENTO
    -- =====================================

    SELECT
        v.IdVenta,
        v.Subtotal,
        v.Descuento,
        v.Total,
        v.Estado,
        pv.Monto AS MontoPagado,
        pv.MedioPago,
        mi.TipoMovimiento,
        mi.Cantidad AS VariacionInventario
    FROM dbo.Venta v
    INNER JOIN dbo.PagoVenta pv
        ON v.IdVenta = pv.IdVenta
    LEFT JOIN dbo.MovimientoInventario mi
        ON v.IdVenta = mi.IdVenta
    WHERE v.IdVenta = @IdVenta;

    -- =====================================
    -- 11. REVERTIR LA SIMULACION
    -- =====================================

    ROLLBACK TRANSACTION;

    -- Verificar que no quedan cambios.
    SELECT
        N'SIMULACION REVERTIDA CORRECTAMENTE'
            AS ResultadoFinal,
        (
            SELECT StockActual
            FROM dbo.Producto
            WHERE IdProducto = @IdCuaderno
        ) AS StockRestaurado,
        (
            SELECT COUNT(*)
            FROM dbo.Venta
            WHERE IdVenta = @IdVenta
        ) AS VentaPersistente;

END TRY
BEGIN CATCH

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    SELECT
        ERROR_NUMBER() AS CodigoError,
        ERROR_MESSAGE() AS MensajeError;

END CATCH;
GO
