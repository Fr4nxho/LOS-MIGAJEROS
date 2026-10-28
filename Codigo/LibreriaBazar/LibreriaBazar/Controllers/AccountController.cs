using System.Security.Claims;
using LibreriaBazar.Data;
using LibreriaBazar.Models;
using LibreriaBazar.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazar.Controllers;

public class AccountController : Controller
{
    private const int MaxIntentos = 3;
    private const int MinutosBloqueo = 5;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;

    public AccountController(AppDbContext db, IPasswordHasher<Usuario> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // RF01: inicio de sesión con clave. RNF11: bloqueo tras 3 intentos fallidos.
    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario == vm.NombreUsuario && u.Activo);

        if (usuario == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(vm);
        }

        if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.Now)
        {
            var minutos = (int)Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.Now).TotalMinutes);
            ModelState.AddModelError(string.Empty,
                $"Acceso bloqueado temporalmente por intentos fallidos. Intente nuevamente en {minutos} minuto(s).");
            return View(vm);
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= MaxIntentos)
            {
                usuario.BloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueo);
                usuario.IntentosFallidos = 0;
                await _db.SaveChangesAsync();
                ModelState.AddModelError(string.Empty,
                    $"Tres intentos incorrectos. El acceso quedó bloqueado por {MinutosBloqueo} minutos.");
            }
            else
            {
                await _db.SaveChangesAsync();
                ModelState.AddModelError(string.Empty,
                    $"Usuario o contraseña incorrectos. Intentos restantes: {MaxIntentos - usuario.IntentosFallidos}.");
            }
            return View(vm);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new("NombreCompleto", usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad));

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return Redirect(vm.ReturnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    // RF02: cerrar sesión
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();
}
