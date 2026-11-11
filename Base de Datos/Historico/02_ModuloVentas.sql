
USE [LibreriaBazarRosita];
GO

-- =========================================
-- TABLA 6: VENTA
-- =========================================

IF OBJECT_ID(N'dbo.Venta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Venta (
        IdVenta INT IDENTITY(1,1) PRIMARY KEY,
        IdUsuario INT NOT NULL,
        FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Subtotal DECIMAL(12,2) NOT NULL,
        Descuento DECIMAL(12,2) NOT NULL DEFAULT 0,
        Total DECIMAL(12,2) NOT NULL,
        Estado NVARCHAR(20) NOT NULL DEFAULT N'PENDIENTE',

        CONSTRAINT FK_Venta_Usuario
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuario(IdUsuario),

        CONSTRAINT CK_Venta_Importes
        CHECK (
            Subtotal >= 0
            AND Descuento >= 0
            AND Descuento <= Subtotal
            AND Total = Subtotal - Descuento
        ),

        CONSTRAINT CK_Venta_Estado
        CHECK (
            Estado IN (
                N'PENDIENTE',
                N'COMPLETADA',
                N'ANULADA'
            )
        )
    );
END;
GO

-- =========================================
-- TABLA 7: DETALLEVENTA
-- =========================================

IF OBJECT_ID(N'dbo.DetalleVenta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetalleVenta (
        IdDetalleVenta INT IDENTITY(1,1) PRIMARY KEY,
        IdVenta INT NOT NULL,
        IdItem INT NOT NULL,
        Cantidad INT NOT NULL,
        PrecioUnitario DECIMAL(12,2) NOT NULL,
        Subtotal DECIMAL(12,2) NOT NULL,

        CONSTRAINT FK_DetalleVenta_Venta
        FOREIGN KEY (IdVenta)
        REFERENCES dbo.Venta(IdVenta),

        CONSTRAINT FK_DetalleVenta_ItemCatalogo
        FOREIGN KEY (IdItem)
        REFERENCES dbo.ItemCatalogo(IdItem),

        CONSTRAINT CK_DetalleVenta_Cantidad
        CHECK (Cantidad > 0),

        CONSTRAINT CK_DetalleVenta_Importes
        CHECK (
            PrecioUnitario >= 0
            AND Subtotal >= 0
            AND Subtotal = Cantidad * PrecioUnitario
        )
    );
END;
GO

-- =========================================
-- TABLA 8: PAGOVENTA
-- =========================================

IF OBJECT_ID(N'dbo.PagoVenta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoVenta (
        IdVenta INT PRIMARY KEY,
        FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Monto DECIMAL(12,2) NOT NULL,
        MedioPago NVARCHAR(30) NOT NULL,

        CONSTRAINT FK_PagoVenta_Venta
        FOREIGN KEY (IdVenta)
        REFERENCES dbo.Venta(IdVenta),

        CONSTRAINT CK_PagoVenta_Monto
        CHECK (Monto >= 0)
    );
END;
GO

-- =========================================
-- VERIFICAR LAS TABLAS CREADAS
-- =========================================

SELECT
    TABLE_NAME AS NombreTabla
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO
