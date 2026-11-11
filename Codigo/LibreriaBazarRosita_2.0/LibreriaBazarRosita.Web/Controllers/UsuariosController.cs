
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibreriaBazarRosita.Web.Controllers;

[Authorize(Roles = "PROPIETARIO")]
public class UsuariosController : Controller
{
    private readonly LibreriaBazarRositaContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public UsuariosController(
        LibreriaBazarRositaContext context,
        IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // =====================================
    // CONSULTAR USUARIOS
    // =====================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarios = await _context.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .ToListAsync();

        return View(usuarios);
    }

    // =====================================
    // MOSTRAR FORMULARIO
    // =====================================

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new RegistrarUsuarioViewModel());
    }

    // =====================================
    // REGISTRAR USUARIO
    // =====================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        RegistrarUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        string nombre = modelo.Nombre.Trim();

        string nombreUsuario = modelo.NombreUsuario
            .Trim()
            .ToLowerInvariant();

        // Verificar que no exista el usuario.
        bool existe = await _context.Usuarios
            .AnyAsync(u =>
                u.NombreUsuario.ToLower() == nombreUsuario);

        if (existe)
        {
            ModelState.AddModelError(
                nameof(modelo.NombreUsuario),
                "Este nombre de usuario ya está registrado.");

            return View(modelo);
        }

        // Solo se crean cuentas ENCARGADO.
        var usuario = new Usuario
        {
            Nombre = nombre,
            NombreUsuario = nombreUsuario,
            Rol = "ENCARGADO",
            Estado = true
        };

        // Almacenar un hash, nunca la contraseña original.
        usuario.ClaveHash = _passwordHasher.HashPassword(
            usuario,
            modelo.Clave);

        try
        {
            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "No se pudo registrar el usuario. " +
                "Verifique los datos e inténtelo nuevamente.");

            return View(modelo);
        }

        TempData["Exito"] =
            "Usuario registrado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================
    // ACTIVAR O DESACTIVAR ENCARGADO
    // =====================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.IdUsuario == id);

        if (usuario == null)
            return NotFound();

        // Comprobar quién realiza la operación.
        var idActualTexto =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(idActualTexto, out int idActual))
            return Forbid();

        // Nadie puede desactivar su propia cuenta.
        if (usuario.IdUsuario == idActual)
        {
            TempData["Info"] =
                "No puede desactivar su propia cuenta.";

            return RedirectToAction(nameof(Index));
        }

        // Este módulo solamente administra encargados.
        if (usuario.Rol != "ENCARGADO")
        {
            TempData["Info"] =
                "No se permite modificar el estado del propietario.";

            return RedirectToAction(nameof(Index));
        }

        usuario.Estado = !usuario.Estado;

        await _context.SaveChangesAsync();

        TempData["Exito"] = usuario.Estado
            ? "Usuario reactivado correctamente."
            : "Usuario desactivado correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // MOSTRAR FORMULARIO DE RESTABLECIMIENTO
    [HttpGet]
    public async Task<IActionResult> RestablecerClave(int id)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.IdUsuario == id &&
                u.Rol == "ENCARGADO");

        if (usuario == null)
            return NotFound();

        return View(new RestablecerClaveViewModel
        {
            IdUsuario = usuario.IdUsuario,
            NombreUsuario = usuario.NombreUsuario
        });
    }

    // GUARDAR LA NUEVA CONTRASEÑA
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestablecerClave(
        RestablecerClaveViewModel modelo)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.IdUsuario == modelo.IdUsuario &&
                u.Rol == "ENCARGADO");

        if (usuario == null)
            return NotFound();

        modelo.NombreUsuario = usuario.NombreUsuario;

        if (!ModelState.IsValid)
            return View(modelo);

        usuario.ClaveHash = _passwordHasher.HashPassword(
            usuario,
            modelo.NuevaClave);

        await _context.SaveChangesAsync();

        TempData["Exito"] =
            "Contraseña restablecida correctamente.";

        return RedirectToAction(nameof(Index));
    }

}
