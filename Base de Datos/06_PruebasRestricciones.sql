
USE [LibreriaBazarRosita];
GO

-- =========================================
-- PRUEBA 1: PRECIO NEGATIVO
-- =========================================

BEGIN TRANSACTION;

BEGIN TRY

    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem)
    VALUES
        (N'Cuaderno de prueba', -5.00, N'PRODUCTO');

    SELECT
        'ERROR: Se permitió un precio negativo'
        AS Resultado;

END TRY
BEGIN CATCH

    SELECT
        'CORRECTO: Se rechazó el precio negativo'
        AS Resultado,
        ERROR_NUMBER() AS CodigoError,
        ERROR_MESSAGE() AS DetalleError;

END CATCH;

-- Revertir cualquier cambio de esta prueba.
IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
GO


USE [LibreriaBazarRosita];
GO

-- =========================================
-- PRUEBA 2: TIPO DE ITEM INVALIDO
-- =========================================

BEGIN TRANSACTION;

BEGIN TRY

    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem)
    VALUES
        (N'Item de prueba', 10.00, N'OTRO');

    SELECT
        'ERROR: Se permitio un tipo de item invalido'
        AS Resultado;

END TRY
BEGIN CATCH

    SELECT
        CASE
            WHEN ERROR_NUMBER() = 547
             AND ERROR_MESSAGE() LIKE '%CK_ItemCatalogo_Tipo%'
            THEN 'CORRECTO: Se rechazo el tipo de item invalido'
            ELSE 'REVISAR: Se produjo otro error'
        END AS Resultado,
        ERROR_NUMBER() AS CodigoError,
        ERROR_MESSAGE() AS DetalleError;

END CATCH;

-- Revertir cualquier cambio.
IF @@TRANCOUNT > 0
    ROLLBACK TRANSACTION;
GO



USE [LibreriaBazarRosita];
GO

-- =========================================
-- PRUEBA 3: STOCK NEGATIVO
-- =========================================

DECLARE @IdCategoriaPrueba INT;
DECLARE @IdItemPrueba INT;

BEGIN TRY

    BEGIN TRANSACTION;

    -- Crear una categoria temporal.
    INSERT INTO dbo.Categoria
        (Nombre, Descripcion)
    VALUES
        (N'Categoria de prueba',
         N'Prueba de integridad del stock');

    SET @IdCategoriaPrueba = SCOPE_IDENTITY();

    -- Crear un item temporal.
    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem)
    VALUES
        (N'Cuaderno de prueba', 5.00, N'PRODUCTO');

    SET @IdItemPrueba = SCOPE_IDENTITY();

    -- Registrar un producto con stock valido.
    INSERT INTO dbo.Producto
        (IdProducto, IdCategoria, StockActual, StockMinimo)
    VALUES
        (@IdItemPrueba, @IdCategoriaPrueba, 10, 2);

    -- Intentar actualizar el stock a un valor negativo.
    UPDATE dbo.Producto
    SET StockActual = -1
    WHERE IdProducto = @IdItemPrueba;

    SELECT
        'ERROR: Se permitio un stock negativo'
        AS Resultado;

    ROLLBACK TRANSACTION;

END TRY
BEGIN CATCH

    SELECT
        CASE
            WHEN ERROR_NUMBER() = 547
             AND ERROR_MESSAGE() LIKE '%CK_Producto_StockActual%'
            THEN 'CORRECTO: Se rechazo el stock negativo'
            ELSE 'REVISAR: Se produjo otro error'
        END AS Resultado,
        ERROR_NUMBER() AS CodigoError,
        ERROR_MESSAGE() AS DetalleError;

    -- Eliminar los cambios temporales.
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

END CATCH;
GO





USE [LibreriaBazarRosita];
GO

-- =========================================
-- RESULTADOS DE LAS PRUEBAS 4 A 7
-- =========================================

IF OBJECT_ID('tempdb..#ResultadosIntegridad') IS NOT NULL
    DROP TABLE #ResultadosIntegridad;

CREATE TABLE #ResultadosIntegridad (
    NumeroPrueba INT,
    Prueba NVARCHAR(100),
    Resultado NVARCHAR(100),
    CodigoError INT NULL,
    DetalleError NVARCHAR(4000) NULL
);
GO

-- =========================================
-- PRUEBA 4: USUARIO INEXISTENTE
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @UsuarioInexistente INT = -1;

    WHILE EXISTS (
        SELECT 1
        FROM dbo.Usuario
        WHERE IdUsuario = @UsuarioInexistente
    )
    BEGIN
        SET @UsuarioInexistente = @UsuarioInexistente - 1;
    END;

    INSERT INTO dbo.Venta
        (IdUsuario, Subtotal, Descuento, Total)
    VALUES
        (@UsuarioInexistente, 10.00, 0.00, 10.00);

    ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        4,
        N'Usuario inexistente',
        N'FALLO: se acepto el registro',
        NULL,
        N'La clave foranea no rechazo el usuario'
    );

END TRY
BEGIN CATCH
    DECLARE @Error4 INT = ERROR_NUMBER();
    DECLARE @Mensaje4 NVARCHAR(4000) = ERROR_MESSAGE();

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        4,
        N'Usuario inexistente',
        CASE
            WHEN @Error4 = 547
             AND @Mensaje4 LIKE N'%FK_Venta_Usuario%'
            THEN N'APROBADA'
            ELSE N'REVISAR'
        END,
        @Error4,
        @Mensaje4
    );
END CATCH;
GO

-- =========================================
-- PRUEBA 5: CANTIDAD DE VENTA INVALIDA
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdUsuario5 INT;
    DECLARE @IdItem5 INT;
    DECLARE @IdVenta5 INT;

    -- Crear usuario temporal.
    INSERT INTO dbo.Usuario
        (Nombre, NombreUsuario, ClaveHash, Rol)
    VALUES (
        N'Usuario prueba 5',
        N'prueba5_' + LEFT(CONVERT(NVARCHAR(36), NEWID()), 8),
        N'HASH_SOLO_PARA_PRUEBA',
        N'ENCARGADO'
    );

    SET @IdUsuario5 = SCOPE_IDENTITY();

    -- Crear item temporal.
    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem)
    VALUES
        (N'Item prueba cantidad', 5.00, N'PRODUCTO');

    SET @IdItem5 = SCOPE_IDENTITY();

    -- Crear venta temporal.
    INSERT INTO dbo.Venta
        (IdUsuario, Subtotal, Descuento, Total)
    VALUES
        (@IdUsuario5, 5.00, 0.00, 5.00);

    SET @IdVenta5 = SCOPE_IDENTITY();

    -- Intentar registrar una cantidad igual a cero.
    INSERT INTO dbo.DetalleVenta
        (IdVenta, IdItem, Cantidad, PrecioUnitario, Subtotal)
    VALUES
        (@IdVenta5, @IdItem5, 0, 5.00, 0.00);

    ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        5,
        N'Cantidad de venta igual a cero',
        N'FALLO: se acepto el registro',
        NULL,
        N'La restriccion permitio cantidad cero'
    );

END TRY
BEGIN CATCH
    DECLARE @Error5 INT = ERROR_NUMBER();
    DECLARE @Mensaje5 NVARCHAR(4000) = ERROR_MESSAGE();

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        5,
        N'Cantidad de venta igual a cero',
        CASE
            WHEN @Error5 = 547
             AND @Mensaje5 LIKE N'%CK_DetalleVenta_Cantidad%'
            THEN N'APROBADA'
            ELSE N'REVISAR'
        END,
        @Error5,
        @Mensaje5
    );
END CATCH;
GO

-- =========================================
-- PRUEBA 6: DESCUENTO MAYOR AL SUBTOTAL
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdUsuario6 INT;

    -- Crear usuario temporal.
    INSERT INTO dbo.Usuario
        (Nombre, NombreUsuario, ClaveHash, Rol)
    VALUES (
        N'Usuario prueba 6',
        N'prueba6_' + LEFT(CONVERT(NVARCHAR(36), NEWID()), 8),
        N'HASH_SOLO_PARA_PRUEBA',
        N'ENCARGADO'
    );

    SET @IdUsuario6 = SCOPE_IDENTITY();

    -- Subtotal = 10, descuento = 11.
    -- Total = -1 mantiene la igualdad aritmetica,
    -- pero el descuento supera el subtotal.
    INSERT INTO dbo.Venta
        (IdUsuario, Subtotal, Descuento, Total)
    VALUES
        (@IdUsuario6, 10.00, 11.00, -1.00);

    ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        6,
        N'Descuento mayor al subtotal',
        N'FALLO: se acepto el registro',
        NULL,
        N'La restriccion permitio el descuento'
    );

END TRY
BEGIN CATCH
    DECLARE @Error6 INT = ERROR_NUMBER();
    DECLARE @Mensaje6 NVARCHAR(4000) = ERROR_MESSAGE();

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        6,
        N'Descuento mayor al subtotal',
        CASE
            WHEN @Error6 = 547
             AND @Mensaje6 LIKE N'%CK_Venta_Importes%'
            THEN N'APROBADA'
            ELSE N'REVISAR'
        END,
        @Error6,
        @Mensaje6
    );
END CATCH;
GO

-- =========================================
-- PRUEBA 7: MOVIMIENTO DE INVENTARIO INVALIDO
-- =========================================

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdUsuario7 INT;
    DECLARE @IdCategoria7 INT;
    DECLARE @IdProducto7 INT;
    DECLARE @IdVenta7 INT;

    -- Crear usuario temporal.
    INSERT INTO dbo.Usuario
        (Nombre, NombreUsuario, ClaveHash, Rol)
    VALUES (
        N'Usuario prueba 7',
        N'prueba7_' + LEFT(CONVERT(NVARCHAR(36), NEWID()), 8),
        N'HASH_SOLO_PARA_PRUEBA',
        N'ENCARGADO'
    );

    SET @IdUsuario7 = SCOPE_IDENTITY();

    -- Crear categoria temporal.
    INSERT INTO dbo.Categoria
        (Nombre, Descripcion)
    VALUES
        (N'Categoria prueba 7', N'Prueba de inventario');

    SET @IdCategoria7 = SCOPE_IDENTITY();

    -- Crear item temporal.
    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem)
    VALUES
        (N'Cuaderno prueba 7', 5.00, N'PRODUCTO');

    SET @IdProducto7 = SCOPE_IDENTITY();

    -- Registrar producto temporal.
    INSERT INTO dbo.Producto
        (IdProducto, IdCategoria, StockActual, StockMinimo)
    VALUES
        (@IdProducto7, @IdCategoria7, 10, 2);

    -- Crear venta temporal.
    INSERT INTO dbo.Venta
        (IdUsuario, Subtotal, Descuento, Total)
    VALUES
        (@IdUsuario7, 5.00, 0.00, 5.00);

    SET @IdVenta7 = SCOPE_IDENTITY();

    -- Una SALIDA debe tener cantidad negativa.
    -- Intentamos registrar una SALIDA positiva.
    INSERT INTO dbo.MovimientoInventario
        (
            IdProducto,
            IdUsuario,
            IdVenta,
            IdPedido,
            TipoMovimiento,
            Cantidad,
            Motivo
        )
    VALUES
        (
            @IdProducto7,
            @IdUsuario7,
            @IdVenta7,
            NULL,
            N'SALIDA',
            3,
            N'Prueba de salida invalida'
        );

    ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        7,
        N'Salida de inventario positiva',
        N'FALLO: se acepto el registro',
        NULL,
        N'La restriccion permitio una salida positiva'
    );

END TRY
BEGIN CATCH
    DECLARE @Error7 INT = ERROR_NUMBER();
    DECLARE @Mensaje7 NVARCHAR(4000) = ERROR_MESSAGE();

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    INSERT INTO #ResultadosIntegridad
    VALUES (
        7,
        N'Salida de inventario positiva',
        CASE
            WHEN @Error7 = 547
             AND @Mensaje7 LIKE N'%CK_Movimiento_Origen%'
            THEN N'APROBADA'
            ELSE N'REVISAR'
        END,
        @Error7,
        @Mensaje7
    );
END CATCH;
GO

-- =========================================
-- RESULTADOS FINALES
-- =========================================

SELECT
    NumeroPrueba,
    Prueba,
    Resultado,
    CodigoError,
    DetalleError
FROM #ResultadosIntegridad
ORDER BY NumeroPrueba;
GO

DROP TABLE #ResultadosIntegridad;
GO

