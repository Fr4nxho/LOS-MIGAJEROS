using System.Globalization;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.Services.Pagos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC (Modelo - Vista - Controlador)
builder.Services.AddControllersWithViews();

// Acceso a datos: Entity Framework Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

// Patrón Strategy: un método de pago nuevo solo requiere una clase IEstrategiaPago y una línea aquí
builder.Services.AddSingleton<IEstrategiaPago, PagoEfectivo>();
builder.Services.AddSingleton<IEstrategiaPago, PagoYapePlin>();
builder.Services.AddSingleton<IEstrategiaPago, PagoTarjeta>();
builder.Services.AddSingleton<SelectorMetodoPago>();

// Autenticación por cookie (RF01, RF02, RNF06)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Account/Login";
        opt.AccessDeniedPath = "/Account/AccesoDenegado";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
    });

// Todo el sistema exige sesión iniciada salvo las acciones marcadas con [AllowAnonymous]
builder.Services.AddAuthorization(opt =>
    opt.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

// Cultura peruana: moneda S/ y fechas dd/MM/yyyy
var cultura = new CultureInfo("es-PE");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = new List<CultureInfo> { cultura },
    SupportedUICultures = new List<CultureInfo> { cultura }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Crea la base de datos la primera vez y carga datos de ejemplo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
    db.Database.EnsureCreated();
    DbInitializer.Seed(db, hasher);
}

app.Run();
