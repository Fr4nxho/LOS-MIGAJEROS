
USE [LibreriaBazarRosita];
GO

-- =========================================
-- TABLA 13: MOVIMIENTOINVENTARIO
-- =========================================

IF OBJECT_ID(N'dbo.MovimientoInventario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientoInventario (
        IdMovimiento INT IDENTITY(1,1) PRIMARY KEY,
        IdProducto INT NOT NULL,
        IdUsuario INT NOT NULL,
        IdVenta INT NULL,
        IdPedido INT NULL,
        FechaHora DATETIME2 NOT NULL
            DEFAULT SYSDATETIME(),
        TipoMovimiento NVARCHAR(20) NOT NULL,
        Cantidad INT NOT NULL,
        Motivo NVARCHAR(255) NULL,

        -- Claves foraneas
        CONSTRAINT FK_Movimiento_Producto
        FOREIGN KEY (IdProducto)
        REFERENCES dbo.Producto(IdProducto),

        CONSTRAINT FK_Movimiento_Usuario
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuario(IdUsuario),

        CONSTRAINT FK_Movimiento_Venta
        FOREIGN KEY (IdVenta)
        REFERENCES dbo.Venta(IdVenta),

        CONSTRAINT FK_Movimiento_PedidoProveedor
        FOREIGN KEY (IdPedido)
        REFERENCES dbo.PedidoProveedor(IdPedido),

        -- Validar los tipos de movimiento
        CONSTRAINT CK_Movimiento_Tipo
        CHECK (
            TipoMovimiento IN (
                N'ENTRADA',
                N'SALIDA',
                N'AJUSTE'
            )
        ),

        -- No registrar movimientos sin variacion
        CONSTRAINT CK_Movimiento_Cantidad
        CHECK (Cantidad <> 0),

        -- Validar el origen de cada movimiento
        CONSTRAINT CK_Movimiento_Origen
        CHECK (
            (
                TipoMovimiento = N'ENTRADA'
                AND Cantidad > 0
                AND IdPedido IS NOT NULL
                AND IdVenta IS NULL
            )
            OR
            (
                TipoMovimiento = N'SALIDA'
                AND Cantidad < 0
                AND IdVenta IS NOT NULL
                AND IdPedido IS NULL
            )
            OR
            (
                TipoMovimiento = N'AJUSTE'
                AND IdVenta IS NULL
                AND IdPedido IS NULL
            )
        )
    );
END;
GO

-- =========================================
-- VERIFICAR LAS 13 TABLAS
-- =========================================

SELECT
    TABLE_NAME AS NombreTabla
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- =========================================
-- VERIFICAR LAS CLAVES FORANEAS
-- =========================================

SELECT
    fk.name AS ClaveForanea,
    OBJECT_NAME(fk.referenced_object_id) AS TablaDestino
FROM sys.foreign_keys AS fk
WHERE fk.parent_object_id =
      OBJECT_ID(N'dbo.MovimientoInventario')
ORDER BY fk.name;
GO
