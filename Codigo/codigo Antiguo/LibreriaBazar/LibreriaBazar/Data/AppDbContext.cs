using LibreriaBazar.Models;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<PagoCliente> PagosCliente => Set<PagoCliente>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();
    public DbSet<PedidoProveedor> Pedidos => Set<PedidoProveedor>();
    public DbSet<DetallePedido> DetallesPedido => Set<DetallePedido>();
    public DbSet<PagoProveedor> PagosProveedor => Set<PagoProveedor>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Por defecto no se borra en cascada (los productos y proveedores se desactivan, no se eliminan)
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        modelBuilder.Entity<Producto>().HasIndex(p => p.Codigo).IsUnique();

        // Las líneas y los pagos sí dependen de su cabecera
        modelBuilder.Entity<Venta>()
            .HasMany(v => v.Detalles).WithOne(d => d.Venta)
            .HasForeignKey(d => d.VentaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Venta>()
            .HasOne(v => v.Pago).WithOne(p => p.Venta)
            .HasForeignKey<PagoCliente>(p => p.VentaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoProveedor>()
            .HasMany(p => p.Detalles).WithOne(d => d.Pedido)
            .HasForeignKey(d => d.PedidoProveedorId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PedidoProveedor>()
            .HasMany(p => p.Pagos).WithOne(d => d.Pedido)
            .HasForeignKey(d => d.PedidoProveedorId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MovimientoInventario>()
            .Property(m => m.Tipo).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<PedidoProveedor>()
            .Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Producto>().Ignore(p => p.StockBajo);
        modelBuilder.Entity<PedidoProveedor>().Ignore(p => p.TotalPagado);
        modelBuilder.Entity<PedidoProveedor>().Ignore(p => p.Saldo);
    }
}
