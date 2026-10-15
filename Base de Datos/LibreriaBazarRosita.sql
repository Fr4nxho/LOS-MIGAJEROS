
create database LibreriaBazarRositaDB

USE LibreriaBazarRositaDB;
GO

/* ====================== SEGURIDAD Y CATÁLOGO ============================== */

-- 1. USUARIO
CREATE TABLE dbo.Usuario (
    id_usuario      INT IDENTITY(1,1) NOT NULL,
    nombre          NVARCHAR(120) NOT NULL,
    nombre_usuario  NVARCHAR(60) NOT NULL,
    contrasena_hash NVARCHAR(255) NOT NULL,
    rol             NVARCHAR(20) NOT NULL,
    estado          BIT NOT NULL CONSTRAINT DF_Usuario_Estado DEFAULT (1),
    CONSTRAINT PK_Usuario PRIMARY KEY (id_usuario),
    CONSTRAINT UQ_Usuario_NombreUsuario UNIQUE (nombre_usuario),
    CONSTRAINT CK_Usuario_Rol CHECK (rol IN (N'ENCARGADO', N'PROPIETARIO'))
);
GO

-- 2. CATEGORIA
CREATE TABLE dbo.Categoria (
    id_categoria INT IDENTITY(1,1) NOT NULL,
    nombre       NVARCHAR(80) NOT NULL,
    descripcion  NVARCHAR(250) NULL,
    estado       BIT NOT NULL CONSTRAINT DF_Categoria_Estado DEFAULT (1),
    CONSTRAINT PK_Categoria PRIMARY KEY (id_categoria),
    CONSTRAINT UQ_Categoria_Nombre UNIQUE (nombre)
);
GO

-- 3. ITEM CATALOGO
CREATE TABLE dbo.ItemCatalogo (
    id_item INT IDENTITY(1,1) NOT NULL,
    nombre  NVARCHAR(120) NOT NULL,
    precio  DECIMAL(10,2) NOT NULL,
    tipo    NVARCHAR(20) NOT NULL,
    estado  BIT NOT NULL CONSTRAINT DF_ItemCatalogo_Estado DEFAULT (1),
    CONSTRAINT PK_ItemCatalogo PRIMARY KEY (id_item),
    CONSTRAINT CK_ItemCatalogo_Precio CHECK (precio >= 0),
    CONSTRAINT CK_ItemCatalogo_Tipo CHECK (tipo IN (N'PRODUCTO', N'SERVICIO'))
);
GO

-- 4. PRODUCTO
CREATE TABLE dbo.Producto (
    id_item      INT NOT NULL,
    id_categoria INT NOT NULL,
    stock        INT NOT NULL CONSTRAINT DF_Producto_Stock DEFAULT (0),
    stock_minimo INT NOT NULL CONSTRAINT DF_Producto_StockMinimo DEFAULT (0),
    CONSTRAINT PK_Producto PRIMARY KEY (id_item),
    CONSTRAINT FK_Producto_ItemCatalogo FOREIGN KEY (id_item)
        REFERENCES dbo.ItemCatalogo (id_item),
    CONSTRAINT FK_Producto_Categoria FOREIGN KEY (id_categoria)
        REFERENCES dbo.Categoria (id_categoria),
    CONSTRAINT CK_Producto_Stock CHECK (stock >= 0),
    CONSTRAINT CK_Producto_StockMinimo CHECK (stock_minimo >= 0)
);
GO

-- 5. SERVICIO
CREATE TABLE dbo.Servicio (
    id_item INT NOT NULL,
    CONSTRAINT PK_Servicio PRIMARY KEY (id_item),
    CONSTRAINT FK_Servicio_ItemCatalogo FOREIGN KEY (id_item)
        REFERENCES dbo.ItemCatalogo (id_item)
);
GO

/* ============================== VENTAS =================================== */

-- 6. VENTA
CREATE TABLE dbo.Venta (
    id_venta   INT IDENTITY(1,1) NOT NULL,
    id_usuario INT NOT NULL,
    fecha_hora DATETIME2(0) NOT NULL CONSTRAINT DF_Venta_Fecha DEFAULT (SYSDATETIME()),
    subtotal   DECIMAL(10,2) NOT NULL,
    descuento  DECIMAL(10,2) NOT NULL CONSTRAINT DF_Venta_Descuento DEFAULT (0),
    total      DECIMAL(10,2) NOT NULL,
    estado     NVARCHAR(20) NOT NULL CONSTRAINT DF_Venta_Estado DEFAULT (N'REGISTRADA'),
    CONSTRAINT PK_Venta PRIMARY KEY (id_venta),
    CONSTRAINT FK_Venta_Usuario FOREIGN KEY (id_usuario)
        REFERENCES dbo.Usuario (id_usuario),
    CONSTRAINT CK_Venta_Importes CHECK (
        subtotal >= 0 AND descuento >= 0 AND descuento <= subtotal
        AND total = subtotal - descuento
    ),
    CONSTRAINT CK_Venta_Estado CHECK (estado IN (N'REGISTRADA', N'ANULADA'))
);
GO

-- 7. DETALLE VENTA
CREATE TABLE dbo.DetalleVenta (
    id_detalle_venta INT IDENTITY(1,1) NOT NULL,
    id_venta         INT NOT NULL,
    id_item          INT NOT NULL,
    cantidad         INT NOT NULL,
    precio_unitario  DECIMAL(10,2) NOT NULL,
    subtotal         DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_DetalleVenta PRIMARY KEY (id_detalle_venta),
    CONSTRAINT FK_DetalleVenta_Venta FOREIGN KEY (id_venta)
        REFERENCES dbo.Venta (id_venta),
    CONSTRAINT FK_DetalleVenta_ItemCatalogo FOREIGN KEY (id_item)
        REFERENCES dbo.ItemCatalogo (id_item),
    CONSTRAINT CK_DetalleVenta_Importes CHECK (
        cantidad > 0 AND precio_unitario >= 0
        AND subtotal = cantidad * precio_unitario
    )
);
GO

-- 8. PAGO VENTA
CREATE TABLE dbo.PagoVenta (
    id_pago_venta INT IDENTITY(1,1) NOT NULL,
    id_venta      INT NOT NULL,
    fecha_hora    DATETIME2(0) NOT NULL CONSTRAINT DF_PagoVenta_Fecha DEFAULT (SYSDATETIME()),
    monto         DECIMAL(10,2) NOT NULL,
    medio_pago    NVARCHAR(40) NOT NULL,
    CONSTRAINT PK_PagoVenta PRIMARY KEY (id_pago_venta),
    CONSTRAINT UQ_PagoVenta_Venta UNIQUE (id_venta),
    CONSTRAINT FK_PagoVenta_Venta FOREIGN KEY (id_venta)
        REFERENCES dbo.Venta (id_venta),
    CONSTRAINT CK_PagoVenta_Monto CHECK (monto >= 0)
);
GO

/* ========================== ABASTECIMIENTO =============================== */

-- 9. PROVEEDOR
CREATE TABLE dbo.Proveedor (
    id_proveedor INT IDENTITY(1,1) NOT NULL,
    nombre       NVARCHAR(120) NOT NULL,
    telefono     NVARCHAR(25) NULL,
    correo       NVARCHAR(120) NULL,
    direccion    NVARCHAR(200) NULL,
    estado       BIT NOT NULL CONSTRAINT DF_Proveedor_Estado DEFAULT (1),
    CONSTRAINT PK_Proveedor PRIMARY KEY (id_proveedor)
);
GO

-- 10. PEDIDO PROVEEDOR
CREATE TABLE dbo.PedidoProveedor (
    id_pedido    INT IDENTITY(1,1) NOT NULL,
    id_proveedor INT NOT NULL,
    id_usuario   INT NOT NULL,
    fecha        DATETIME2(0) NOT NULL CONSTRAINT DF_Pedido_Fecha DEFAULT (SYSDATETIME()),
    estado       NVARCHAR(20) NOT NULL CONSTRAINT DF_Pedido_Estado DEFAULT (N'PENDIENTE'),
    total        DECIMAL(10,2) NOT NULL CONSTRAINT DF_Pedido_Total DEFAULT (0),
    CONSTRAINT PK_PedidoProveedor PRIMARY KEY (id_pedido),
    CONSTRAINT FK_PedidoProveedor_Proveedor FOREIGN KEY (id_proveedor)
        REFERENCES dbo.Proveedor (id_proveedor),
    CONSTRAINT FK_PedidoProveedor_Usuario FOREIGN KEY (id_usuario)
        REFERENCES dbo.Usuario (id_usuario),
    CONSTRAINT CK_PedidoProveedor_Estado CHECK (
        estado IN (N'PENDIENTE', N'RECIBIDO', N'CANCELADO')
    ),
    CONSTRAINT CK_PedidoProveedor_Total CHECK (total >= 0)
);
GO

-- 11. DETALLE PEDIDO
CREATE TABLE dbo.DetallePedido (
    id_detalle_pedido INT IDENTITY(1,1) NOT NULL,
    id_pedido         INT NOT NULL,
    id_item_producto  INT NOT NULL,
    cantidad          INT NOT NULL,
    costo_unitario    DECIMAL(10,2) NOT NULL,
    subtotal          DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_DetallePedido PRIMARY KEY (id_detalle_pedido),
    CONSTRAINT FK_DetallePedido_Pedido FOREIGN KEY (id_pedido)
        REFERENCES dbo.PedidoProveedor (id_pedido),
    CONSTRAINT FK_DetallePedido_Producto FOREIGN KEY (id_item_producto)
        REFERENCES dbo.Producto (id_item),
    CONSTRAINT CK_DetallePedido_Importes CHECK (
        cantidad > 0 AND costo_unitario >= 0
        AND subtotal = cantidad * costo_unitario
    )
);
GO

-- 12. PAGO PROVEEDOR
CREATE TABLE dbo.PagoProveedor (
    id_pago_proveedor INT IDENTITY(1,1) NOT NULL,
    id_pedido         INT NOT NULL,
    fecha             DATETIME2(0) NOT NULL CONSTRAINT DF_PagoProveedor_Fecha DEFAULT (SYSDATETIME()),
    monto             DECIMAL(10,2) NOT NULL,
    medio_pago        NVARCHAR(40) NOT NULL,
    CONSTRAINT PK_PagoProveedor PRIMARY KEY (id_pago_proveedor),
    CONSTRAINT UQ_PagoProveedor_Pedido UNIQUE (id_pedido),
    CONSTRAINT FK_PagoProveedor_Pedido FOREIGN KEY (id_pedido)
        REFERENCES dbo.PedidoProveedor (id_pedido),
    CONSTRAINT CK_PagoProveedor_Monto CHECK (monto >= 0)
);
GO

/* ============================== INVENTARIO =============================== */

-- 13. MOVIMIENTO INVENTARIO
CREATE TABLE dbo.MovimientoInventario (
    id_movimiento    INT IDENTITY(1,1) NOT NULL,
    id_item_producto INT NOT NULL,
    id_usuario       INT NOT NULL,
    fecha_hora       DATETIME2(0) NOT NULL CONSTRAINT DF_Movimiento_Fecha DEFAULT (SYSDATETIME()),
    tipo_movimiento  NVARCHAR(20) NOT NULL,
    cantidad         INT NOT NULL,
    motivo           NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_MovimientoInventario PRIMARY KEY (id_movimiento),
    CONSTRAINT FK_MovimientoInventario_Producto FOREIGN KEY (id_item_producto)
        REFERENCES dbo.Producto (id_item),
    CONSTRAINT FK_MovimientoInventario_Usuario FOREIGN KEY (id_usuario)
        REFERENCES dbo.Usuario (id_usuario),
    CONSTRAINT CK_MovimientoInventario_Tipo CHECK (
        tipo_movimiento IN (N'ENTRADA', N'SALIDA', N'AJUSTE')
    ),
    CONSTRAINT CK_MovimientoInventario_Cantidad CHECK (
        (tipo_movimiento = N'ENTRADA' AND cantidad > 0)
        OR (tipo_movimiento = N'SALIDA' AND cantidad < 0)
        OR (tipo_movimiento = N'AJUSTE' AND cantidad <> 0)
    )
);
GO
