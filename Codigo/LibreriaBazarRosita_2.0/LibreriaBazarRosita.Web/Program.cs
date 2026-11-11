using System.Security.Cryptography;
using System.Text;
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

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

        options.Events.OnValidatePrincipal = async context =>
        {
            var idTexto = context.Principal?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                ?.Value;

            var rolCookie = context.Principal?
                .FindFirst(System.Security.Claims.ClaimTypes.Role)
                ?.Value;

            var versionCookie = context.Principal?
                .FindFirst("ClaveVersion")?.Value;

            var db = context.HttpContext.RequestServices
                .GetRequiredService<LibreriaBazarRositaContext>();

            Usuario? usuarioActual = null;

            if (int.TryParse(idTexto, out int idUsuario))
            {
                usuarioActual = await db.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
            }

            bool valido = false;

            if (usuarioActual != null &&
                usuarioActual.Estado &&
                usuarioActual.Rol == rolCookie &&
                !string.IsNullOrEmpty(versionCookie))
            {
                var versionActual = Convert.ToHexString(
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(usuarioActual.ClaveHash)
                    )
                );

                valido = string.Equals(
                    versionCookie,
                    versionActual,
                    StringComparison.Ordinal);
            }

            if (!valido)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };


    });

// ==========================================
// 5. REGISTRAR AUTORIZACION
// ==========================================


builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});


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
    pattern: "{controller=Catalogo}/{action=Index}/{id?}"
);


app.Run();
