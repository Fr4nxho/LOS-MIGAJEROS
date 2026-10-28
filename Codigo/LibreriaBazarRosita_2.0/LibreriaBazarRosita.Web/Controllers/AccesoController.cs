
using System.Security.Claims;
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazarRosita.Web.Controllers
{
    public class AccesoController : Controller
    {
        private readonly LibreriaBazarRositaContext _context;
        private readonly IPasswordHasher<Usuario> _passwordHasher;

        public AccesoController(
            LibreriaBazarRositaContext context,
            IPasswordHasher<Usuario> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Mostrar formulario de inicio de sesión.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult IniciarSesion(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Catalogo");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View(new IniciarSesionViewModel());
        }

        // Validar credenciales e iniciar sesión.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(
            IniciarSesionViewModel modelo,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            string nombreUsuario = modelo.NombreUsuario.Trim();

            // Buscar al usuario activo.
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.NombreUsuario == nombreUsuario &&
                    u.Estado);

            // Verificar el hash de la contraseña.
            var resultado = usuario == null
                ? PasswordVerificationResult.Failed
                : _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.ClaveHash,
                    modelo.Clave
                );

            if (usuario == null ||
                resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Usuario o contraseña incorrectos."
                );

                return View(modelo);
            }

            // Preparar los datos de identidad.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    usuario.NombreUsuario
                ),

                new Claim(
                    ClaimTypes.Role,
                    usuario.Rol
                )
            };

            var identidad = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identidad);

            // Crear la sesión mediante cookie.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false
                }
            );

            // Permitir únicamente redirecciones locales.
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Catalogo");
        }

        // Cerrar sesión.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("IniciarSesion");
        }

        // Pantalla para usuarios sin permisos.
        [HttpGet]
        public IActionResult Denegado()
        {
            return View();
        }
    }
}
