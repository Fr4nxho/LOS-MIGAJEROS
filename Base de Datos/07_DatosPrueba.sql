
USE [LibreriaBazarRosita];
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- =====================================
    -- 1. CATEGORIAS
    -- =====================================

    DECLARE @Categorias TABLE (
        Nombre NVARCHAR(80),
        Descripcion NVARCHAR(255)
    );

    INSERT INTO @Categorias VALUES
    (N'Cuadernos', N'Cuadernos escolares y universitarios'),
    (N'Escritura', N'Lapiceros y utiles de escritura'),
    (N'Accesorios', N'Accesorios escolares');

    INSERT INTO dbo.Categoria (Nombre, Descripcion)
    SELECT c.Nombre, c.Descripcion
    FROM @Categorias c
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Categoria x
        WHERE x.Nombre = c.Nombre
    );

    -- =====================================
    -- 2. PRODUCTOS DE PRUEBA
    -- =====================================

    DECLARE @Productos TABLE (
        Nombre NVARCHAR(120),
        Precio DECIMAL(12,2),
        Categoria NVARCHAR(80),
        StockActual INT,
        StockMinimo INT
    );

    INSERT INTO @Productos VALUES
    (N'Cuaderno A4', 6.50, N'Cuadernos', 40, 10),
    (N'Lapicero azul', 1.50, N'Escritura', 80, 15),
    (N'Borrador blanco', 1.00, N'Accesorios', 30, 5);

    -- Registrar los productos en el catalogo.
    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem, Estado)
    SELECT
        p.Nombre, p.Precio, N'PRODUCTO', 1
    FROM @Productos p
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.ItemCatalogo i
        WHERE i.Nombre = p.Nombre
          AND i.TipoItem = N'PRODUCTO'
    );

    -- Registrar la informacion de inventario.
    INSERT INTO dbo.Producto
        (IdProducto, IdCategoria, StockActual, StockMinimo)
    SELECT
        i.IdItem,
        c.IdCategoria,
        p.StockActual,
        p.StockMinimo
    FROM @Productos p
    INNER JOIN dbo.ItemCatalogo i
        ON i.Nombre = p.Nombre
       AND i.TipoItem = N'PRODUCTO'
    INNER JOIN dbo.Categoria c
        ON c.Nombre = p.Categoria
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Producto x
        WHERE x.IdProducto = i.IdItem
    );

    -- =====================================
    -- 3. SERVICIOS DE PRUEBA
    -- =====================================

    DECLARE @Servicios TABLE (
        Nombre NVARCHAR(120),
        Precio DECIMAL(12,2)
    );

    INSERT INTO @Servicios VALUES
    (N'Impresion', 0.50),
    (N'Fotocopia', 0.20);

    -- Registrar servicios en el catalogo.
    INSERT INTO dbo.ItemCatalogo
        (Nombre, Precio, TipoItem, Estado)
    SELECT
        s.Nombre, s.Precio, N'SERVICIO', 1
    FROM @Servicios s
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.ItemCatalogo i
        WHERE i.Nombre = s.Nombre
          AND i.TipoItem = N'SERVICIO'
    );

    -- Registrar su subtipo Servicio.
    INSERT INTO dbo.Servicio (IdServicio)
    SELECT i.IdItem
    FROM @Servicios s
    INNER JOIN dbo.ItemCatalogo i
        ON i.Nombre = s.Nombre
       AND i.TipoItem = N'SERVICIO'
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Servicio x
        WHERE x.IdServicio = i.IdItem
    );

    -- =====================================
    -- 4. PROVEEDOR DE PRUEBA
    -- =====================================

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Proveedor
        WHERE Nombre = N'Proveedor de demostracion'
    )
    BEGIN
        INSERT INTO dbo.Proveedor
            (Nombre, Estado)
        VALUES
            (N'Proveedor de demostracion', 1);
    END;

    COMMIT TRANSACTION;

    PRINT 'Datos de prueba registrados correctamente';

END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

-- =====================================
-- 5. VERIFICAR CATALOGO E INVENTARIO
-- =====================================

SELECT
    i.IdItem,
    i.Nombre,
    i.TipoItem,
    i.Precio,
    c.Nombre AS Categoria,
    p.StockActual,
    p.StockMinimo
FROM dbo.ItemCatalogo i
LEFT JOIN dbo.Producto p
    ON i.IdItem = p.IdProducto
LEFT JOIN dbo.Categoria c
    ON p.IdCategoria = c.IdCategoria
ORDER BY i.IdItem;
GO

-- =====================================
-- 6. VERIFICAR PROVEEDORES
-- =====================================

SELECT
    IdProveedor,
    Nombre,
    Estado
FROM dbo.Proveedor
ORDER BY IdProveedor;
GO

-- =====================================
-- 7. VERIFICAR CATEGORIAS
-- =====================================

SELECT
    IdCategoria,
    Nombre,
    Descripcion
FROM dbo.Categoria
ORDER BY IdCategoria;
GO
