
USE [LibreriaBazarRosita];
GO

-- =========================================
-- TABLA 9: PROVEEDOR
-- =========================================

IF OBJECT_ID(N'dbo.Proveedor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Proveedor (
        IdProveedor INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(120) NOT NULL,
        Telefono NVARCHAR(20) NULL,
        Correo NVARCHAR(120) NULL,
        Direccion NVARCHAR(200) NULL,
        Estado BIT NOT NULL DEFAULT 1
    );
END;
GO

-- =========================================
-- TABLA 10: PEDIDOPROVEEDOR
-- =========================================

IF OBJECT_ID(N'dbo.PedidoProveedor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PedidoProveedor (
        IdPedido INT IDENTITY(1,1) PRIMARY KEY,
        IdProveedor INT NOT NULL,
        IdUsuario INT NOT NULL,
        Fecha DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Estado NVARCHAR(20) NOT NULL DEFAULT N'PENDIENTE',
        Total DECIMAL(12,2) NOT NULL,

        CONSTRAINT FK_PedidoProveedor_Proveedor
        FOREIGN KEY (IdProveedor)
        REFERENCES dbo.Proveedor(IdProveedor),

        CONSTRAINT FK_PedidoProveedor_Usuario
        FOREIGN KEY (IdUsuario)
        REFERENCES dbo.Usuario(IdUsuario),

        CONSTRAINT CK_PedidoProveedor_Estado
        CHECK (
            Estado IN (
                N'PENDIENTE',
                N'RECIBIDO',
                N'CANCELADO'
            )
        ),

        CONSTRAINT CK_PedidoProveedor_Total
        CHECK (Total >= 0)
    );
END;
GO

-- =========================================
-- TABLA 11: DETALLEPEDIDO
-- =========================================

IF OBJECT_ID(N'dbo.DetallePedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DetallePedido (
        IdDetallePedido INT IDENTITY(1,1) PRIMARY KEY,
        IdPedido INT NOT NULL,
        IdProducto INT NOT NULL,
        Cantidad INT NOT NULL,
        CostoUnitario DECIMAL(12,2) NOT NULL,
        Subtotal DECIMAL(12,2) NOT NULL,

        CONSTRAINT FK_DetallePedido_PedidoProveedor
        FOREIGN KEY (IdPedido)
        REFERENCES dbo.PedidoProveedor(IdPedido),

        CONSTRAINT FK_DetallePedido_Producto
        FOREIGN KEY (IdProducto)
        REFERENCES dbo.Producto(IdProducto),

        CONSTRAINT CK_DetallePedido_Cantidad
        CHECK (Cantidad > 0),

        CONSTRAINT CK_DetallePedido_Importes
        CHECK (
            CostoUnitario >= 0
            AND Subtotal >= 0
            AND Subtotal = Cantidad * CostoUnitario
        )
    );
END;
GO

-- =========================================
-- TABLA 12: PAGOPROVEEDOR
-- =========================================

IF OBJECT_ID(N'dbo.PagoProveedor', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PagoProveedor (
        IdPedido INT PRIMARY KEY,
        FechaHora DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        Monto DECIMAL(12,2) NOT NULL,
        MedioPago NVARCHAR(30) NOT NULL,

        CONSTRAINT FK_PagoProveedor_PedidoProveedor
        FOREIGN KEY (IdPedido)
        REFERENCES dbo.PedidoProveedor(IdPedido),

        CONSTRAINT CK_PagoProveedor_Monto
        CHECK (Monto > 0)
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
