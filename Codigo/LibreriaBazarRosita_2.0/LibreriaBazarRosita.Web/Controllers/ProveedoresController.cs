
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazarRosita.Web.Controllers;

[Authorize(Roles = "PROPIETARIO,ENCARGADO")]
public class ProveedoresController : Controller
{
    private readonly LibreriaBazarRositaContext _context;

    public ProveedoresController(
        LibreriaBazarRositaContext context)
    {
        _context = context;
    }

    // 1. Consultar proveedores.
    [HttpGet]
    public async Task<IActionResult> Index(
        string? busqueda,
        bool incluirInactivos = false)
    {
        var consulta = _context.Set<Proveedor>()
            .AsNoTracking()
            .AsQueryable();

        if (!incluirInactivos)
        {
            consulta = consulta.Where(p => p.Estado);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.Trim();

            consulta = consulta.Where(p =>
                p.Nombre.Contains(busqueda));
        }

        ViewBag.Busqueda = busqueda ?? "";
        ViewBag.IncluirInactivos = incluirInactivos;

        var proveedores = await consulta
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        return View(proveedores);
    }

    // 2. Mostrar formulario.
    [HttpGet]
    public IActionResult Crear()
    {
        return View(new ProveedorFormViewModel());
    }

    // 3. Registrar proveedor.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
        ProveedorFormViewModel modelo)
    {
        modelo.Nombre = modelo.Nombre?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(modelo.Nombre))
        {
            ModelState.AddModelError(
                nameof(modelo.Nombre),
                "Ingrese un nombre válido.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var proveedor = new Proveedor
        {
            Nombre = modelo.Nombre,
            Telefono = string.IsNullOrWhiteSpace(modelo.Telefono)
                ? null : modelo.Telefono.Trim(),
            Correo = string.IsNullOrWhiteSpace(modelo.Correo)
                ? null : modelo.Correo.Trim(),
            Direccion = string.IsNullOrWhiteSpace(modelo.Direccion)
                ? null : modelo.Direccion.Trim(),
            Estado = true
        };

        try
        {
            _context.Set<Proveedor>().Add(proveedor);

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Proveedor registrado correctamente.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "No se pudo registrar el proveedor.");

            return View(modelo);
        }
    }

    // ==========================================
    // MOSTRAR FORMULARIO DE EDICIÓN
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var proveedor = await _context.Set<Proveedor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdProveedor == id);

        if (proveedor == null)
        {
            return NotFound();
        }

        var modelo = new ProveedorFormViewModel
        {
            Nombre = proveedor.Nombre,
            Telefono = proveedor.Telefono,
            Correo = proveedor.Correo,
            Direccion = proveedor.Direccion
        };

        return View(modelo);
    }

    // ==========================================
    // GUARDAR CAMBIOS DEL PROVEEDOR
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        int id,
        ProveedorFormViewModel modelo)
    {
        modelo.Nombre = modelo.Nombre?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(modelo.Nombre))
        {
            ModelState.AddModelError(
                nameof(modelo.Nombre),
                "Ingrese un nombre válido.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var proveedor = await _context.Set<Proveedor>()
            .FindAsync(id);

        if (proveedor == null)
        {
            return NotFound();
        }

        proveedor.Nombre = modelo.Nombre;

        proveedor.Telefono =
            string.IsNullOrWhiteSpace(modelo.Telefono)
            ? null : modelo.Telefono.Trim();

        proveedor.Correo =
            string.IsNullOrWhiteSpace(modelo.Correo)
            ? null : modelo.Correo.Trim();

        proveedor.Direccion =
            string.IsNullOrWhiteSpace(modelo.Direccion)
            ? null : modelo.Direccion.Trim();

        try
        {
            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "Proveedor actualizado correctamente.";

            return RedirectToAction(
                nameof(Index),
                new { incluirInactivos = true });
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                "",
                "No se pudieron guardar los cambios.");

            return View(modelo);
        }
    }

    // ==========================================
    // DESACTIVAR O REACTIVAR PROVEEDOR
    // ==========================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var proveedor = await _context.Set<Proveedor>()
            .FindAsync(id);

        if (proveedor == null)
        {
            return NotFound();
        }

        proveedor.Estado = !proveedor.Estado;

        await _context.SaveChangesAsync();

        TempData["Exito"] = proveedor.Estado
            ? "Proveedor reactivado correctamente."
            : "Proveedor desactivado correctamente.";

        return RedirectToAction(
            nameof(Index),
            new { incluirInactivos = true });
    }

}
