
using LibreriaBazarRosita.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using System.Text;


namespace LibreriaBazarRosita.Web.Controllers
{
    [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
    public class InventarioController : Controller
    {
        private readonly LibreriaBazarRositaContext _context;

        public InventarioController(
            LibreriaBazarRositaContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CONSULTAR STOCK
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? busqueda,
            string? nivelStock)
        {
            // Consultar solo productos activos.
            var consulta = _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .Where(i =>
                    i.Estado &&
                    i.TipoItem == "PRODUCTO" &&
                    i.Producto != null);

            // ======================================
            // 2. BUSCAR PRODUCTOS POR NOMBRE
            // ======================================

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                consulta = consulta.Where(i =>
                    i.Nombre.Contains(busqueda));
            }

            // ======================================
            // 3. FILTRAR SEGÚN EL NIVEL DE STOCK
            // ======================================

            nivelStock = nivelStock?.Trim().ToUpperInvariant();

            switch (nivelStock)
            {
                case "AGOTADO":

                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual == 0);

                    break;

                case "BAJO":

                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual > 0 &&
                        i.Producto.StockActual <=
                        i.Producto.StockMinimo);

                    break;

                case "DISPONIBLE":

                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual >
                        i.Producto.StockMinimo);

                    break;

                default:

                    nivelStock = "";

                    break;
            }

            // ======================================
            // 4. OBTENER LOS RESULTADOS
            // ======================================

            var productos = await consulta
                .OrderBy(i => i.Nombre)
                .ToListAsync();

            ViewBag.Busqueda = busqueda;
            ViewBag.NivelStock = nivelStock;

            return View(productos);
        }

        // ==========================================
        // 2. MOSTRAR FORMULARIO DE AJUSTE
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Ajustar(int id)
        {
            var item = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i =>
                    i.IdItem == id &&
                    i.Estado &&
                    i.TipoItem == "PRODUCTO" &&
                    i.Producto != null);

            if (item == null)
            {
                return NotFound();
            }

            var modelo = new AjustarInventarioViewModel
            {
                IdProducto = item.IdItem,
                NombreProducto = item.Nombre,
                StockActual = item.Producto!.StockActual,
                StockMinimo = item.Producto.StockMinimo,
                CantidadFisica = item.Producto.StockActual
            };

            return View(modelo);
        }

        // ==========================================
        // 3. REGISTRAR AJUSTE DE INVENTARIO
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ajustar(
            AjustarInventarioViewModel modelo)
        {
            if (modelo.IdProducto <= 0)
            {
                return BadRequest();
            }

            // Consultar los datos reales de SQL Server.
            var item = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i =>
                    i.IdItem == modelo.IdProducto &&
                    i.Estado &&
                    i.TipoItem == "PRODUCTO" &&
                    i.Producto != null);

            if (item == null)
            {
                return NotFound();
            }

            // Recuperar información para mostrar el formulario.
            modelo.NombreProducto = item.Nombre;
            modelo.StockMinimo = item.Producto!.StockMinimo;
            modelo.Motivo = modelo.Motivo?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(modelo.Motivo))
            {
                ModelState.AddModelError(
                    nameof(modelo.Motivo),
                    "Ingrese un motivo válido.");
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            // Comprobar que el stock no cambió mientras
            // el usuario tenía abierto el formulario.
            if (modelo.StockActual != item.Producto.StockActual)
            {
                TempData["Info"] =
                    "El stock cambió recientemente. Revise " +
                    "la cantidad actual antes de realizar el ajuste.";

                return RedirectToAction(
                    nameof(Ajustar),
                    new { id = modelo.IdProducto });
            }

            int diferencia =
                modelo.CantidadFisica!.Value - modelo.StockActual;

            // Si el conteo coincide, no existe ajuste pendiente.
            if (diferencia == 0)
            {
                TempData["Info"] =
                    "El stock físico coincide con el registrado. " +
                    "No se realizaron cambios.";

                return RedirectToAction(nameof(Index));
            }

            // Identificar al usuario autenticado.
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            // Actualizar el stock y registrar el movimiento
            // en una sola transacción.
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualización condicionada al stock anterior.
                // Evita sobrescribir otro ajuste simultáneo.
                int filasActualizadas =
                    await _context.Set<Producto>()
                    .Where(p =>
                        p.IdProducto == modelo.IdProducto &&
                        p.StockActual == modelo.StockActual)
                    .ExecuteUpdateAsync(actualizacion =>
                        actualizacion.SetProperty(
                            p => p.StockActual,
                            modelo.CantidadFisica.Value));

                if (filasActualizadas != 1)
                {
                    await transaccion.RollbackAsync();

                    TempData["Info"] =
                        "Otro usuario modificó el stock. " +
                        "Vuelva a revisar el producto.";

                    return RedirectToAction(
                        nameof(Ajustar),
                        new { id = modelo.IdProducto });
                }

                // Registrar la diferencia en el historial.
                var movimiento = new MovimientoInventario
                {
                    IdProducto = modelo.IdProducto,
                    IdUsuario = idUsuario,
                    IdVenta = null,
                    IdPedido = null,
                    FechaHora = DateTime.Now,
                    TipoMovimiento = "AJUSTE",
                    Cantidad = diferencia,
                    Motivo = modelo.Motivo
                };

                _context.MovimientoInventarios.Add(movimiento);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                TempData["Exito"] =
                    "El ajuste de inventario fue registrado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "No se pudo registrar el ajuste. " +
                    "Verifique los datos e inténtelo nuevamente.");

                return View(modelo);
            }
        }


        // ==========================================
        // 4. GENERAR REPORTE DE INVENTARIO
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Reporte(
            string? busqueda,
            string? estadoStock)
        {
            // Obtener productos activos con su categoría.
            var consulta = _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                    .ThenInclude(p => p!.IdCategoriaNavigation)
                .Where(i =>
                    i.Estado &&
                    i.TipoItem == "PRODUCTO" &&
                    i.Producto != null);

            // Filtrar por nombre.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                consulta = consulta.Where(i =>
                    i.Nombre.Contains(busqueda));
            }

            // Filtrar por estado de existencias.
            estadoStock = estadoStock?.Trim().ToUpperInvariant();

            switch (estadoStock)
            {
                case "AGOTADO":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual == 0);
                    break;

                case "BAJO":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual > 0 &&
                        i.Producto.StockActual <=
                        i.Producto.StockMinimo);
                    break;

                case "DISPONIBLE":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual >
                        i.Producto.StockMinimo);
                    break;

                default:
                    estadoStock = "";
                    break;
            }

            // Generar resultados ordenados.
            var productos = await consulta
                .OrderBy(i => i.Nombre)
                .ToListAsync();

            ViewBag.Busqueda = busqueda;
            ViewBag.EstadoStock = estadoStock;
            ViewBag.FechaGeneracion = DateTime.Now;

            return View(productos);
        }

        // ==========================================
        // 5. EXPORTAR REPORTE DE INVENTARIO
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Exportar(
            string? busqueda,
            string? estadoStock,
            string formato = "CSV")
        {
            // 1. Verificar el formato solicitado.
            if (!string.Equals(
                formato,
                "CSV",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Info"] =
                    "El formato de exportación no es válido.";

                return RedirectToAction(
                    nameof(Reporte),
                    new { busqueda, estadoStock });
            }

            // 2. Consultar únicamente productos activos.
            var consulta = _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                    .ThenInclude(p => p!.IdCategoriaNavigation)
                .Where(i =>
                    i.Estado &&
                    i.TipoItem == "PRODUCTO" &&
                    i.Producto != null);

            // 3. Aplicar el filtro de nombre.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                consulta = consulta.Where(i =>
                    i.Nombre.Contains(busqueda));
            }

            // 4. Aplicar el filtro de stock.
            estadoStock = estadoStock?.Trim().ToUpperInvariant();

            switch (estadoStock)
            {
                case "AGOTADO":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual == 0);
                    break;

                case "BAJO":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual > 0 &&
                        i.Producto.StockActual <=
                        i.Producto.StockMinimo);
                    break;

                case "DISPONIBLE":
                    consulta = consulta.Where(i =>
                        i.Producto!.StockActual >
                        i.Producto.StockMinimo);
                    break;

                default:
                    estadoStock = "";
                    break;
            }

            // 5. Obtener los productos.
            var productos = await consulta
                .OrderBy(i => i.Nombre)
                .ToListAsync();

            if (productos.Count == 0)
            {
                TempData["Info"] =
                    "No existen productos para exportar " +
                    "con los filtros seleccionados.";

                return RedirectToAction(
                    nameof(Reporte),
                    new { busqueda, estadoStock });
            }

            // 6. Construir el archivo CSV.
            var csv = new StringBuilder();

            csv.AppendLine(
                "ID;Producto;Categoria;StockActual;" +
                "StockMinimo;Faltantes;Estado");

            foreach (var item in productos)
            {
                var producto = item.Producto!;

                int faltantes = Math.Max(
                    0,
                    producto.StockMinimo -
                    producto.StockActual);

                string estado;

                if (producto.StockActual == 0)
                {
                    estado = "AGOTADO";
                }
                else if (producto.StockActual <=
                         producto.StockMinimo)
                {
                    estado = "STOCK BAJO";
                }
                else
                {
                    estado = "DISPONIBLE";
                }

                string categoria =
                    producto.IdCategoriaNavigation?.Nombre
                    ?? "Sin categoria";

                string fila = string.Join(";",
                    item.IdItem,
                    EscaparCsv(item.Nombre),
                    EscaparCsv(categoria),
                    producto.StockActual,
                    producto.StockMinimo,
                    faltantes,
                    estado);

                csv.AppendLine(fila);
            }

            // 7. Codificar en UTF-8 con BOM para Excel.
            string contenido = "\uFEFF" + csv.ToString();

            byte[] archivo = Encoding.UTF8.GetBytes(contenido);

            string nombreArchivo =
                $"ReporteInventario_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // 8. Entregar el archivo para descarga.
            return File(
                archivo,
                "text/csv; charset=utf-8",
                nombreArchivo);
        }

        // Protege los valores de texto dentro del CSV.
        private static string EscaparCsv(string? valor)
        {
            valor ??= "";

            valor = valor
                .Replace("\r", " ")
                .Replace("\n", " ");

            string inicio = valor.TrimStart();

            if (inicio.Length > 0 &&
                "=+-@".Contains(inicio[0]))
            {
                valor = "'" + valor;
            }

            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        }

    }
}
