using LibreriaBazar.Models;
using Microsoft.AspNetCore.Identity;

namespace LibreriaBazar.Data;

/// <summary>Datos iniciales para poder usar el sistema desde el primer arranque.</summary>
public static class DbInitializer
{
    public static void Seed(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        if (!db.Usuarios.Any())
        {
            var propietario = new Usuario
            {
                NombreUsuario = "propietario",
                NombreCompleto = "Propietario",
                Rol = Roles.Propietario
            };
            propietario.PasswordHash = hasher.HashPassword(propietario, "Propietario123");

            var encargado = new Usuario
            {
                NombreUsuario = "encargado",
                NombreCompleto = "Encargado / Vendedor",
                Rol = Roles.Encargado
            };
            encargado.PasswordHash = hasher.HashPassword(encargado, "Encargado123");

            db.Usuarios.AddRange(propietario, encargado);
            db.SaveChanges();
        }

        if (!db.Categorias.Any())
        {
            db.Categorias.AddRange(
                new Categoria { Nombre = "Cuadernos" },
                new Categoria { Nombre = "Escritura" },
                new Categoria { Nombre = "Papelería" },
                new Categoria { Nombre = "Útiles escolares" },
                new Categoria { Nombre = "Oficina" },
                new Categoria { Nombre = "Otros" });
            db.SaveChanges();
        }

        if (!db.Productos.Any())
        {
            var cat = db.Categorias.ToDictionary(c => c.Nombre, c => c.Id);

            db.Productos.AddRange(
                new Producto { Codigo = "CUA-001", Nombre = "Cuaderno cuadriculado 100 hojas", CategoriaId = cat["Cuadernos"], PrecioVenta = 4.50m, CostoUnitario = 3.00m, Stock = 40, StockMinimo = 10 },
                new Producto { Codigo = "CUA-002", Nombre = "Cuaderno rayado 100 hojas", CategoriaId = cat["Cuadernos"], PrecioVenta = 4.50m, CostoUnitario = 3.00m, Stock = 35, StockMinimo = 10 },
                new Producto { Codigo = "ESC-001", Nombre = "Lapicero azul", CategoriaId = cat["Escritura"], PrecioVenta = 1.00m, CostoUnitario = 0.50m, Stock = 120, StockMinimo = 30 },
                new Producto { Codigo = "ESC-002", Nombre = "Lápiz HB", CategoriaId = cat["Escritura"], PrecioVenta = 0.80m, CostoUnitario = 0.40m, Stock = 100, StockMinimo = 30 },
                new Producto { Codigo = "PAP-001", Nombre = "Papel bond A4 (paquete 100 hojas)", CategoriaId = cat["Papelería"], PrecioVenta = 8.00m, CostoUnitario = 5.50m, Stock = 25, StockMinimo = 8 },
                new Producto { Codigo = "PAP-002", Nombre = "Cartulina blanca", CategoriaId = cat["Papelería"], PrecioVenta = 1.50m, CostoUnitario = 0.80m, Stock = 4, StockMinimo = 10 },
                new Producto { Codigo = "UTI-001", Nombre = "Tijera escolar", CategoriaId = cat["Útiles escolares"], PrecioVenta = 3.50m, CostoUnitario = 2.00m, Stock = 18, StockMinimo = 5 },
                new Producto { Codigo = "UTI-002", Nombre = "Goma en barra", CategoriaId = cat["Útiles escolares"], PrecioVenta = 2.50m, CostoUnitario = 1.40m, Stock = 22, StockMinimo = 6 },
                new Producto { Codigo = "OFI-001", Nombre = "Folder manila A4", CategoriaId = cat["Oficina"], PrecioVenta = 0.60m, CostoUnitario = 0.30m, Stock = 60, StockMinimo = 20 });
            db.SaveChanges();
        }

        if (!db.Servicios.Any())
        {
            db.Servicios.AddRange(
                new Servicio { Nombre = "Fotocopia B/N", Descripcion = "Por hoja", Precio = 0.10m },
                new Servicio { Nombre = "Impresión B/N", Descripcion = "Por hoja", Precio = 0.30m },
                new Servicio { Nombre = "Impresión a color", Descripcion = "Por hoja", Precio = 1.00m },
                new Servicio { Nombre = "Anillado", Descripcion = "Por documento", Precio = 3.00m },
                new Servicio { Nombre = "Escaneo", Descripcion = "Por hoja", Precio = 0.50m });
            db.SaveChanges();
        }

        if (!db.Proveedores.Any())
        {
            db.Proveedores.Add(new Proveedor
            {
                Nombre = "Distribuidora de Útiles (ejemplo)",
                Telefono = "999999999",
                Direccion = "Lima"
            });
            db.SaveChanges();
        }
    }
}
