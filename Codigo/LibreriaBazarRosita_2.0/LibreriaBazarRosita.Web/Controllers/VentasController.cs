
using System.Globalization;
using System.Text;
using System.Security.Claims;
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibreriaBazarRosita.Web.Controllers
{
    [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
    public class VentasController : Controller
    {
        private readonly LibreriaBazarRositaContext _context;
        private readonly ILogger<VentasController> _logger;

        private const decimal MaximoImporte = 9999999999.99m;

        public VentasController(
            LibreriaBazarRositaContext context,
            ILogger<VentasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // 1. MOSTRAR FORMULARIO DE VENTA
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarItemsAsync();

            return View(new RegistrarVentaViewModel());
        }

        // ==========================================
        // 2. REGISTRAR LA VENTA COMPLETA
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            RegistrarVentaViewModel modelo)
        {
            // Obtener el usuario autenticado.
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }

            // Validar medio de pago.
            string medioPago =
                modelo.MedioPago?.Trim().ToUpperInvariant() ?? "";

            if (medioPago is not
                ("EFECTIVO" or "YAPE" or "PLIN" or "TRANSFERENCIA"))
            {
                ModelState.AddModelError(
                    nameof(modelo.MedioPago),
                    "Seleccione un medio de pago válido.");
            }

            // Validar descuento monetario.
            if (modelo.Descuento < 0 ||
                modelo.Descuento > MaximoImporte ||
                decimal.Round(modelo.Descuento, 2) != modelo.Descuento)
            {
                ModelState.AddModelError(
                    nameof(modelo.Descuento),
                    "El descuento debe ser válido y tener " +
                    "como máximo dos decimales.");
            }

            if (modelo.Detalles == null ||
                modelo.Detalles.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "Agregue al menos un producto o servicio.");

                return await MostrarFormulario(modelo);
            }

            if (modelo.Detalles.Count > 100)
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "La venta supera la cantidad máxima de líneas.");
            }

            if (!ModelState.IsValid)
            {
                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 3. AGRUPAR ÍTEMS REPETIDOS
            // ======================================

            // Ejemplo:
            // Cuaderno A4 x2 + Cuaderno A4 x3
            // Se procesa como Cuaderno A4 x5.

            var agrupados = modelo.Detalles
                .GroupBy(d => d.IdItem)
                .Select(g => new
                {
                    IdItem = g.Key,
                    Cantidad = g.Sum(d => (long)d.Cantidad)
                })
                .ToList();

            if (agrupados.Any(d =>
                d.IdItem <= 0 ||
                d.Cantidad <= 0 ||
                d.Cantidad > int.MaxValue))
            {
                ModelState.AddModelError(
                    nameof(modelo.Detalles),
                    "Existen artículos o cantidades inválidas.");

                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 4. CONSULTAR PRECIOS REALES
            // ======================================

            var ids = agrupados
                .Select(d => d.IdItem)
                .ToList();

            var items = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .Include(i => i.Servicio)
                .Where(i => ids.Contains(i.IdItem))
                .ToDictionaryAsync(i => i.IdItem);

            var lineas = new List<(
                int IdItem,
                string Nombre,
                int Cantidad,
                decimal Precio,
                decimal Subtotal,
                bool EsProducto)>();

            decimal subtotalVenta = 0m;

            foreach (var detalle in agrupados)
            {
                if (!items.TryGetValue(
                    detalle.IdItem,
                    out var item))
                {
                    ModelState.AddModelError(
                        "",
                        $"No existe el ítem {detalle.IdItem}.");

                    continue;
                }

                if (!item.Estado)
                {
                    ModelState.AddModelError(
                        "",
                        $"El ítem {item.Nombre} está desactivado.");

                    continue;
                }

                bool esProducto = item.TipoItem == "PRODUCTO";
                bool esServicio = item.TipoItem == "SERVICIO";

                if ((!esProducto && !esServicio) ||
                    (esProducto && item.Producto == null) ||
                    (esServicio && item.Servicio == null))
                {
                    ModelState.AddModelError(
                        "",
                        $"El ítem {item.Nombre} no tiene " +
                        "una configuración válida.");

                    continue;
                }

                int cantidad = (int)detalle.Cantidad;

                // Solo los productos necesitan existencias.
                if (esProducto &&
                    item.Producto!.StockActual < cantidad)
                {
                    ModelState.AddModelError(
                        "",
                        $"Stock insuficiente para {item.Nombre}. " +
                        $"Disponible: {item.Producto.StockActual}.");

                    continue;
                }

                if (item.Precio < 0)
                {
                    ModelState.AddModelError(
                        "",
                        $"El precio de {item.Nombre} no es válido.");

                    continue;
                }

                decimal subtotalLinea;

                try
                {
                    subtotalLinea = checked(
                        item.Precio * cantidad);
                }
                catch (OverflowException)
                {
                    ModelState.AddModelError(
                        "",
                        $"El importe de {item.Nombre} es demasiado alto.");

                    continue;
                }

                if (subtotalLinea > MaximoImporte ||
                    subtotalVenta > MaximoImporte - subtotalLinea)
                {
                    ModelState.AddModelError(
                        "",
                        "El importe de la venta excede el máximo permitido.");

                    continue;
                }

                subtotalVenta += subtotalLinea;

                lineas.Add((
                    item.IdItem,
                    item.Nombre,
                    cantidad,
                    item.Precio,
                    subtotalLinea,
                    esProducto
                ));
            }

            if (!ModelState.IsValid)
            {
                return await MostrarFormulario(modelo);
            }

            // ======================================
            // 5. CALCULAR TOTAL DEFINITIVO
            // ======================================

            if (modelo.Descuento > subtotalVenta)
            {
                ModelState.AddModelError(
                    nameof(modelo.Descuento),
                    "El descuento no puede superar el subtotal.");

                return await MostrarFormulario(modelo);
            }

            decimal totalVenta =
                subtotalVenta - modelo.Descuento;

            DateTime fechaHora = DateTime.Now;

            // ======================================
            // 6. INICIAR TRANSACCIÓN
            // ======================================

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // PASO A: Crear venta principal.
                var venta = new Ventum
                {
                    IdUsuario = idUsuario,
                    FechaHora = fechaHora,
                    Subtotal = subtotalVenta,
                    Descuento = modelo.Descuento,
                    Total = totalVenta,
                    Estado = "COMPLETADA"
                };

                _context.Set<Ventum>().Add(venta);

                // Obtener el IdVenta generado por SQL Server.
                await _context.SaveChangesAsync();

                // PASO B: Crear detalles.
                foreach (var linea in lineas)
                {
                    _context.Set<DetalleVentum>().Add(
                        new DetalleVentum
                        {
                            IdVenta = venta.IdVenta,
                            IdItem = linea.IdItem,
                            Cantidad = linea.Cantidad,
                            PrecioUnitario = linea.Precio,
                            Subtotal = linea.Subtotal
                        });
                }

                // PASO C: Registrar pago del cliente.
                _context.Set<PagoVentum>().Add(
                    new PagoVentum
                    {
                        IdVenta = venta.IdVenta,
                        FechaHora = fechaHora,
                        Monto = totalVenta,
                        MedioPago = medioPago
                    });

                // PASO D: Actualizar stock de productos.
                foreach (var linea in lineas.Where(l => l.EsProducto))
                {
                    // Condición importante:
                    // Solo descontar si sigue existiendo
                    // suficiente stock al confirmar la venta.
                    int filasActualizadas =
                        await _context.Set<Producto>()
                            .Where(p =>
                                p.IdProducto == linea.IdItem &&
                                p.StockActual >= linea.Cantidad)
                            .ExecuteUpdateAsync(actualizacion =>
                                actualizacion.SetProperty(
                                    p => p.StockActual,
                                    p => p.StockActual - linea.Cantidad));

                    if (filasActualizadas != 1)
                    {
                        // Deshacer toda la venta.
                        await transaccion.RollbackAsync();

                        ModelState.AddModelError(
                            "",
                            $"El stock de {linea.Nombre} cambió " +
                            "durante el registro. Intente nuevamente.");

                        return await MostrarFormulario(modelo);
                    }

                    // PASO E: Registrar movimiento de salida.
                    _context.Set<MovimientoInventario>().Add(
                        new MovimientoInventario
                        {
                            IdProducto = linea.IdItem,
                            IdUsuario = idUsuario,
                            IdVenta = venta.IdVenta,
                            IdPedido = null,
                            FechaHora = fechaHora,
                            TipoMovimiento = "SALIDA",
                            Cantidad = -linea.Cantidad,
                            Motivo = "VENTA"
                        });
                }

                // Guardar detalles, pago y movimientos.
                await _context.SaveChangesAsync();

                // PASO F: Confirmar todos los cambios.
                await transaccion.CommitAsync();

                TempData["Exito"] =
                    $"Venta N.º {venta.IdVenta} registrada correctamente. " +
                    $"Total: S/ {totalVenta:0.00}.";

                return RedirectToAction(nameof(Crear));
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();

                _logger.LogError(
                    ex,
                    "Error al registrar venta del usuario {IdUsuario}",
                    idUsuario);

                ModelState.AddModelError(
                    "",
                    "No se pudo completar la venta. " +
                    "No se confirmaron cambios en la base de datos.");

                return await MostrarFormulario(modelo);
            }
        }

        // ==========================================
        // 7. CARGAR CATÁLOGO DISPONIBLE
        // ==========================================

        private async Task CargarItemsAsync()
        {
            ViewBag.Items = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .Include(i => i.Servicio)
                .Where(i =>
                    i.Estado &&
                    (
                        (i.TipoItem == "SERVICIO" &&
                         i.Servicio != null)
                        ||
                        (i.TipoItem == "PRODUCTO" &&
                         i.Producto != null &&
                         i.Producto.StockActual > 0)
                    ))
                .OrderBy(i => i.Nombre)
                .ToListAsync();
        }

        // ==========================================
        // 8. REGRESAR AL FORMULARIO CON ERRORES
        // ==========================================

        private async Task<IActionResult> MostrarFormulario(
            RegistrarVentaViewModel modelo)
        {
            await CargarItemsAsync();

            return View("Crear", modelo);
        }

        [HttpGet]
        [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
        public async Task<IActionResult> Historial(
            DateTime? desde,
            DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue &&
                desde.Value.Date > hasta.Value.Date)
            {
                TempData["Info"] =
                    "La fecha inicial no puede ser posterior a la fecha final.";

                return RedirectToAction(nameof(Historial));
            }

            var consulta = _context.Set<Ventum>()
                .AsNoTracking()
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.PagoVentum)
                .AsQueryable();

            if (desde.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.FechaHora >= desde.Value.Date);
            }

            if (hasta.HasValue)
            {
                DateTime fechaLimite = hasta.Value.Date.AddDays(1);

                consulta = consulta.Where(v =>
                    v.FechaHora < fechaLimite);
            }

            var ventas = await consulta
                .OrderByDescending(v => v.FechaHora)
                .ThenByDescending(v => v.IdVenta)
                .ToListAsync();

            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            return View(ventas);
        }

        // ========================================
        // CONSULTAR RESUMEN DIARIO DE VENTAS
        // ========================================

        [HttpGet]
        [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
        public async Task<IActionResult> ResumenDiario(
            DateTime? fecha)
        {
            DateTime fechaConsulta = (fecha ?? DateTime.Today).Date;

            if (fechaConsulta >= DateTime.MaxValue.Date)
            {
                return BadRequest("Fecha no válida.");
            }

            DateTime fechaSiguiente = fechaConsulta.AddDays(1);

            // Solo se consideran ventas completadas.
            var ventas = await _context.Set<Ventum>()
                .AsNoTracking()
                .Include(v => v.PagoVentum)
                .Where(v =>
                    v.Estado == "COMPLETADA" &&
                    v.FechaHora >= fechaConsulta &&
                    v.FechaHora < fechaSiguiente)
                .ToListAsync();

            var modelo = new ResumenDiarioViewModel
            {
                Fecha = fechaConsulta,

                CantidadVentas = ventas.Count,

                Subtotal = ventas.Sum(v => v.Subtotal),

                Descuentos = ventas.Sum(v => v.Descuento),

                Ingresos = ventas.Sum(v => v.Total),

                MediosPago = ventas
                    .GroupBy(v =>
                        v.PagoVentum?.MedioPago
                        ?? "SIN REGISTRAR")
                    .Select(grupo => new ResumenMedioPagoViewModel
                    {
                        MedioPago = grupo.Key,
                        Cantidad = grupo.Count(),
                        Total = grupo.Sum(v => v.Total)
                    })
                    .OrderBy(x => x.MedioPago)
                    .ToList()
            };

            return View(modelo);
        }

        // ========================================
        // GENERAR REPORTE DE VENTAS
        // ========================================

        [HttpGet]
        [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
        public async Task<IActionResult> Reporte(
            DateTime? desde,
            DateTime? hasta)
        {
            var d = (desde ?? DateTime.Today.AddDays(-6)).Date;
            var h = (hasta ?? DateTime.Today).Date;

            if (d > h || h == DateTime.MaxValue.Date)
            {
                TempData["Info"] = "Seleccione un rango de fechas válido.";
                return RedirectToAction(nameof(Historial));
            }

            var modelo = await ObtenerReporteVentas(d, h);

            return View(modelo);
        }

        // ========================================
        // EXPORTAR RESUMEN DE VENTAS
        // ========================================

        [HttpGet]
        [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
        public async Task<IActionResult> ExportarResumen(
            DateTime? desde,
            DateTime? hasta)
        {
            var d = (desde ?? DateTime.Today.AddDays(-6)).Date;
            var h = (hasta ?? DateTime.Today).Date;

            if (d > h || h == DateTime.MaxValue.Date)
            {
                return BadRequest("Rango de fechas no válido.");
            }

            var reporte = await ObtenerReporteVentas(d, h);

            if (reporte.CantidadVentas == 0)
            {
                TempData["Info"] =
                    "No hay ventas para exportar en las fechas seleccionadas.";

                return RedirectToAction(
                    nameof(Reporte),
                    new
                    {
                        desde = d.ToString("yyyy-MM-dd"),
                        hasta = h.ToString("yyyy-MM-dd")
                    });
            }

            var cultura = new CultureInfo("es-PE");
            var csv = new StringBuilder();

            csv.AppendLine(
                "Fecha;Ventas;Subtotal;Descuentos;Ingresos");

            foreach (var dia in reporte.PorDia)
            {
                csv.AppendLine(
                    $"{dia.Fecha:dd/MM/yyyy};" +
                    $"{dia.Cantidad};" +
                    $"{dia.Subtotal.ToString("0.00", cultura)};" +
                    $"{dia.Descuentos.ToString("0.00", cultura)};" +
                    $"{dia.Ingresos.ToString("0.00", cultura)}");
            }

            csv.AppendLine(
                $"TOTAL;{reporte.CantidadVentas};" +
                $"{reporte.Subtotal.ToString("0.00", cultura)};" +
                $"{reporte.Descuentos.ToString("0.00", cultura)};" +
                $"{reporte.Ingresos.ToString("0.00", cultura)}");

            // UTF-8 con BOM para compatibilidad con Excel.
            byte[] contenido = Encoding.UTF8.GetBytes(csv.ToString());

            byte[] archivo = Encoding.UTF8.GetPreamble()
                .Concat(contenido)
                .ToArray();

            string nombreArchivo =
                $"ResumenVentas_{d:yyyyMMdd}_{h:yyyyMMdd}.csv";

            return File(
                archivo,
                "text/csv; charset=utf-8",
                nombreArchivo);
        }

        // ========================================
        // OBTENER DATOS DEL REPORTE
        // ========================================

        private async Task<ReporteVentasViewModel> ObtenerReporteVentas(
            DateTime desde,
            DateTime hasta)
        {
            DateTime fechaLimite = hasta.AddDays(1);

            var registros = await _context.Set<Ventum>()
                .AsNoTracking()
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.PagoVentum)
                .Where(v =>
                    v.Estado == "COMPLETADA" &&
                    v.FechaHora >= desde &&
                    v.FechaHora < fechaLimite)
                .OrderByDescending(v => v.FechaHora)
                .ThenByDescending(v => v.IdVenta)
                .ToListAsync();

            return new ReporteVentasViewModel
            {
                Desde = desde,
                Hasta = hasta,

                Ventas = registros
                    .Select(v => new VentaReporteFilaViewModel
                    {
                        IdVenta = v.IdVenta,
                        FechaHora = v.FechaHora,
                        Usuario = v.IdUsuarioNavigation?.NombreUsuario
                            ?? "No disponible",
                        MedioPago = v.PagoVentum?.MedioPago
                            ?? "No registrado",
                        Subtotal = v.Subtotal,
                        Descuento = v.Descuento,
                        Total = v.Total
                    })
                    .ToList()
            };
        }

    }
}
