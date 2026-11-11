
USE [LibreriaBazarRosita];
GO

SET XACT_ABORT ON;
GO

-- =========================================
-- SIMULACION DE ABASTECIMIENTO
-- Compra de 10 cuadernos A4
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdUsuario INT;
    DECLARE @IdProveedor INT;
    DECLARE @IdProducto INT;
    DECLARE @IdPedido INT;
    DECLARE @StockAntes INT;

    DECLARE @Cantidad INT = 10;
    DECLARE @CostoUnitario DECIMAL(12,2) = 4.50;
    DECLARE @TotalPedido DECIMAL(12,2);

    SET @TotalPedido = @Cantidad * @CostoUnitario;

    -- =====================================
    -- 1. BUSCAR PROVEEDOR
    -- =====================================

    SELECT TOP (1)
        @IdProveedor = IdProveedor
    FROM dbo.Proveedor
    WHERE Nombre = N'Proveedor de demostracion'
      AND Estado = 1
    ORDER BY IdProveedor;

    IF @IdProveedor IS NULL
    BEGIN
        RAISERROR(
            'No existe el proveedor de demostracion.',
            16, 1
        );
    END;

    -- =====================================
    -- 2. BUSCAR PRODUCTO Y STOCK
    -- =====================================

    SELECT TOP (1)
        @IdProducto = p.IdProducto,
        @StockAntes = p.StockActual
    FROM dbo.Producto p
    INNER JOIN dbo.ItemCatalogo i
        ON p.IdProducto = i.IdItem
    WHERE i.Nombre = N'Cuaderno A4'
      AND i.TipoItem = N'PRODUCTO'
      AND i.Estado = 1
    ORDER BY p.IdProducto;

    IF @IdProducto IS NULL
    BEGIN
        RAISERROR(
            'No existe el producto Cuaderno A4.',
            16, 1
        );
    END;

    -- =====================================
    -- 3. CREAR USUARIO TEMPORAL
    -- =====================================

    INSERT INTO dbo.Usuario
        (Nombre, NombreUsuario, ClaveHash, Rol, Estado)
    VALUES (
        N'Encargado de simulacion',
        N'abast_' + CONVERT(NVARCHAR(36), NEWID()),
        N'NO_ES_UN_HASH_VALIDO_SOLO_PRUEBA',
        N'ENCARGADO',
        1
    );

    SET @IdUsuario = SCOPE_IDENTITY();

    -- =====================================
    -- 4. REGISTRAR PEDIDO
    -- =====================================

    INSERT INTO dbo.PedidoProveedor
        (IdProveedor, IdUsuario, Estado, Total)
    VALUES (
        @IdProveedor,
        @IdUsuario,
        N'PENDIENTE',
        @TotalPedido
    );

    SET @IdPedido = SCOPE_IDENTITY();

    -- =====================================
    -- 5. REGISTRAR DETALLE DEL PEDIDO
    -- =====================================

    INSERT INTO dbo.DetallePedido
        (
            IdPedido,
            IdProducto,
            Cantidad,
            CostoUnitario,
            Subtotal
        )
    VALUES (
        @IdPedido,
        @IdProducto,
        @Cantidad,
        @CostoUnitario,
        @TotalPedido
    );

    -- =====================================
    -- 6. REGISTRAR PAGO AL PROVEEDOR
    -- =====================================

    INSERT INTO dbo.PagoProveedor
        (IdPedido, Monto, MedioPago)
    VALUES (
        @IdPedido,
        @TotalPedido,
        N'EFECTIVO'
    );

    -- =====================================
    -- 7. REGISTRAR RECEPCION DE MERCADERIA
    -- =====================================

    UPDATE dbo.Producto
    SET StockActual = StockActual + @Cantidad
    WHERE IdProducto = @IdProducto
      AND StockActual = @StockAntes;

    IF @@ROWCOUNT <> 1
    BEGIN
        RAISERROR(
            'No se pudo actualizar el stock.',
            16, 1
        );
    END;

    -- =====================================
    -- 8. REGISTRAR ENTRADA DE INVENTARIO
    -- =====================================

    INSERT INTO dbo.MovimientoInventario
        (
            IdProducto,
            IdUsuario,
            IdPedido,
            TipoMovimiento,
            Cantidad,
            Motivo
        )
    VALUES (
        @IdProducto,
        @IdUsuario,
        @IdPedido,
        N'ENTRADA',
        @Cantidad,
        N'Recepcion de 10 cuadernos A4'
    );

    -- =====================================
    -- 9. MARCAR PEDIDO COMO RECIBIDO
    -- =====================================

    UPDATE dbo.PedidoProveedor
    SET Estado = N'RECIBIDO'
    WHERE IdPedido = @IdPedido
      AND Estado = N'PENDIENTE';

    IF @@ROWCOUNT <> 1
    BEGIN
        RAISERROR(
            'No se pudo confirmar la recepcion.',
            16, 1
        );
    END;

    -- =====================================
    -- 10. VERIFICAR PEDIDO Y PAGO
    -- =====================================

    SELECT
        ped.IdPedido,
        pro.Nombre AS Proveedor,
        ped.Estado,
        dp.Cantidad,
        dp.CostoUnitario,
        ped.Total,
        pag.Monto AS MontoPagado,
        pag.MedioPago
    FROM dbo.PedidoProveedor ped
    INNER JOIN dbo.Proveedor pro
        ON ped.IdProveedor = pro.IdProveedor
    INNER JOIN dbo.DetallePedido dp
        ON ped.IdPedido = dp.IdPedido
    INNER JOIN dbo.PagoProveedor pag
        ON ped.IdPedido = pag.IdPedido
    WHERE ped.IdPedido = @IdPedido;

    -- =====================================
    -- 11. VERIFICAR STOCK Y MOVIMIENTO
    -- =====================================

    SELECT
        i.Nombre AS Producto,
        @StockAntes AS StockInicial,
        p.StockActual AS StockDuranteSimulacion,
        m.TipoMovimiento,
        m.Cantidad AS VariacionInventario
    FROM dbo.Producto p
    INNER JOIN dbo.ItemCatalogo i
        ON p.IdProducto = i.IdItem
    INNER JOIN dbo.MovimientoInventario m
        ON p.IdProducto = m.IdProducto
    WHERE m.IdPedido = @IdPedido;

    -- =====================================
    -- 12. REVERTIR SIMULACION
    -- =====================================

    ROLLBACK TRANSACTION;

    -- =====================================
    -- 13. VERIFICAR RESTAURACION
    -- =====================================

    SELECT
        N'SIMULACION REVERTIDA CORRECTAMENTE'
            AS ResultadoFinal,
        (
            SELECT StockActual
            FROM dbo.Producto
            WHERE IdProducto = @IdProducto
        ) AS StockRestaurado,
        (
            SELECT COUNT(*)
            FROM dbo.PedidoProveedor
            WHERE IdPedido = @IdPedido
        ) AS PedidoPersistente,
        (
            SELECT COUNT(*)
            FROM dbo.MovimientoInventario
            WHERE IdPedido = @IdPedido
        ) AS MovimientoPersistente;

END TRY
BEGIN CATCH

    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    SELECT
        ERROR_NUMBER() AS CodigoError,
        ERROR_MESSAGE() AS MensajeError;

END CATCH;
GO
