
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. REGISTRAR MVC
// ==========================================

builder.Services.AddControllersWithViews();

// ==========================================
// 2. REGISTRAR ENTITY FRAMEWORK CORE
// ==========================================

builder.Services.AddDbContext<LibreriaBazarRositaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "LibreriaBazarRosita"
        )
    )
);

// ==========================================
// 3. REGISTRAR HASH DE CONTRASEÑAS
// ==========================================

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>
>();

// ==========================================
// 4. CONFIGURAR AUTENTICACION CON COOKIES
// ==========================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme
    )
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/IniciarSesion";

        options.AccessDeniedPath = "/Acceso/Denegado";

        options.Cookie.Name = "LibreriaBazarRosita.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

// ==========================================
// 5. REGISTRAR AUTORIZACION
// ==========================================

builder.Services.AddAuthorization();

var app = builder.Build();

// ==========================================
// 6. CONFIGURAR MIDDLEWARE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Primero identificar al usuario.
app.UseAuthentication();

// Después comprobar sus permisos.
app.UseAuthorization();

// ==========================================
// 7. CONFIGURAR RUTAS MVC
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
