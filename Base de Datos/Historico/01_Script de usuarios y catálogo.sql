
USE [LibreriaBazarRosita];
GO

-- =========================================
-- TABLA 1: USUARIO
-- =========================================

IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario (
        IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(120) NOT NULL,
        NombreUsuario NVARCHAR(50) NOT NULL UNIQUE,
        ClaveHash NVARCHAR(255) NOT NULL,
        Rol NVARCHAR(20) NOT NULL,
        Estado BIT NOT NULL DEFAULT 1,

        CONSTRAINT CK_Usuario_Rol
        CHECK (Rol IN (N'ENCARGADO', N'PROPIETARIO'))
    );
END;
GO

-- =========================================
-- TABLA 2: CATEGORIA
-- =========================================

IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categoria (
        IdCategoria INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(80) NOT NULL,
        Descripcion NVARCHAR(255) NULL
    );
END;
GO

-- =========================================
-- TABLA 3: ITEMCATALOGO
-- =========================================

IF OBJECT_ID(N'dbo.ItemCatalogo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemCatalogo (
        IdItem INT IDENTITY(1,1) PRIMARY KEY,
        Nombre NVARCHAR(120) NOT NULL,
        Precio DECIMAL(12,2) NOT NULL,
        TipoItem NVARCHAR(10) NOT NULL,
        Estado BIT NOT NULL DEFAULT 1,

        CONSTRAINT CK_ItemCatalogo_Precio
        CHECK (Precio >= 0),

        CONSTRAINT CK_ItemCatalogo_Tipo
        CHECK (TipoItem IN (N'PRODUCTO', N'SERVICIO'))
    );
END;
GO

-- =========================================
-- TABLA 4: PRODUCTO
-- =========================================

IF OBJECT_ID(N'dbo.Producto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Producto (
        IdProducto INT PRIMARY KEY,
        IdCategoria INT NOT NULL,
        StockActual INT NOT NULL DEFAULT 0,
        StockMinimo INT NOT NULL DEFAULT 0,

        CONSTRAINT FK_Producto_ItemCatalogo
        FOREIGN KEY (IdProducto)
        REFERENCES dbo.ItemCatalogo(IdItem),

        CONSTRAINT FK_Producto_Categoria
        FOREIGN KEY (IdCategoria)
        REFERENCES dbo.Categoria(IdCategoria),

        CONSTRAINT CK_Producto_StockActual
        CHECK (StockActual >= 0),

        CONSTRAINT CK_Producto_StockMinimo
        CHECK (StockMinimo >= 0)
    );
END;
GO

-- =========================================
-- TABLA 5: SERVICIO
-- =========================================

IF OBJECT_ID(N'dbo.Servicio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Servicio (
        IdServicio INT PRIMARY KEY,

        CONSTRAINT FK_Servicio_ItemCatalogo
        FOREIGN KEY (IdServicio)
        REFERENCES dbo.ItemCatalogo(IdItem)
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
