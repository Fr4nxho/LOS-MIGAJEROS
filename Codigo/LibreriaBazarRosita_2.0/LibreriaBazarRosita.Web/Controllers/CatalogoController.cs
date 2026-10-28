
using LibreriaBazarRosita.Web.Data;
using LibreriaBazarRosita.Web.Models.Entities;
using LibreriaBazarRosita.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace LibreriaBazarRosita.Web.Controllers
{
    [Authorize(Roles = "PROPIETARIO,ENCARGADO")]
    public class CatalogoController : Controller
    {
        private readonly LibreriaBazarRositaContext _context;

        public CatalogoController(
            LibreriaBazarRositaContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CONSULTAR Y BUSCAR CATÁLOGO
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? busqueda,
            string? tipo)
        {
            var consulta = _context.ItemCatalogos
                .AsNoTracking()
                .Include(item => item.Producto)
                .Where(item => item.Estado)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                consulta = consulta.Where(
                    item => item.Nombre.Contains(busqueda)
                );
            }

            tipo = tipo?.Trim().ToUpperInvariant();

            if (tipo == "PRODUCTO" || tipo == "SERVICIO")
            {
                consulta = consulta.Where(
                    item => item.TipoItem == tipo
                );
            }
            else
            {
                tipo = "";
            }

            ViewBag.Busqueda = busqueda;
            ViewBag.Tipo = tipo;

            var items = await consulta
                .OrderBy(item => item.Nombre)
                .ToListAsync();

            return View(items);
        }

        // ==========================================
        // 2. MOSTRAR FORMULARIO DE REGISTRO
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            await CargarCategorias();

            return View(new CrearItemViewModel());
        }

        // ==========================================
        // 3. GUARDAR PRODUCTO O SERVICIO
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(
            CrearItemViewModel modelo)
        {
            string nombre = modelo.Nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ingrese un nombre válido."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarCategorias(modelo.IdCategoria);
                return View(modelo);
            }

            if (modelo.TipoItem == "PRODUCTO")
            {
                bool categoriaExiste = await _context
                    .Set<Categorium>()
                    .AnyAsync(c =>
                        c.IdCategoria == modelo.IdCategoria!.Value);

                if (!categoriaExiste)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "La categoría seleccionada no existe."
                    );
                }
            }

            bool duplicado = await _context.ItemCatalogos
                .AnyAsync(item =>
                    item.Nombre == nombre &&
                    item.TipoItem == modelo.TipoItem);

            if (duplicado)
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ya existe un ítem de este tipo con ese nombre."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarCategorias(modelo.IdCategoria);
                return View(modelo);
            }


            // Obtener el identificador del usuario autenticado.
            if (!int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out int idUsuario))
            {
                return Forbid();
            }


            var itemNuevo = new ItemCatalogo
            {
                Nombre = nombre,
                Precio = modelo.Precio,
                TipoItem = modelo.TipoItem,
                Estado = true
            };

            if (modelo.TipoItem == "PRODUCTO")
            {
                itemNuevo.Producto = new Producto
                {
                    IdCategoria = modelo.IdCategoria!.Value,
                    StockActual = modelo.StockInicial!.Value,
                    StockMinimo = modelo.StockMinimo!.Value
                };
            }
            else
            {
                itemNuevo.Servicio = new Servicio();
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Registrar el producto o servicio.
                _context.ItemCatalogos.Add(itemNuevo);

                await _context.SaveChangesAsync();

                // 2. Registrar el stock inicial solamente
                // cuando sea un producto con cantidad positiva.
                if (modelo.TipoItem == "PRODUCTO" &&
                    modelo.StockInicial.GetValueOrDefault() > 0)
                {
                    var movimientoInicial = new MovimientoInventario
                    {
                        IdProducto = itemNuevo.Producto!.IdProducto,
                        IdUsuario = idUsuario,

                        // No proviene de una venta ni de un pedido.
                        IdVenta = null,
                        IdPedido = null,

                        FechaHora = DateTime.Now,
                        TipoMovimiento = "AJUSTE",
                        Cantidad = modelo.StockInicial!.Value,
                        Motivo = "STOCK_INICIAL"
                    };

                    _context.MovimientoInventarios.Add(
                        movimientoInicial
                    );

                    await _context.SaveChangesAsync();
                }
                // 3. Confirmar ambas operaciones.
                await transaccion.CommitAsync();

                TempData["Exito"] =
                    "El ítem fue registrado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "No se pudo guardar el ítem. Revise los datos."
                );

                await CargarCategorias(modelo.IdCategoria);

                return View(modelo);
            }
        }

        // ==========================================
        // 4. MOSTRAR FORMULARIO DE EDICIÓN
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var item = await _context.ItemCatalogos
                .AsNoTracking()
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i =>
                    i.IdItem == id && i.Estado);

            if (item == null)
            {
                return NotFound();
            }

            if (item.TipoItem == "PRODUCTO" &&
                item.Producto == null)
            {
                return Problem(
                    "El producto no tiene datos de inventario."
                );
            }

            // Convertir los datos de SQL Server
            // al modelo utilizado por el formulario.
            var modelo = new EditarItemViewModel
            {
                IdItem = item.IdItem,
                TipoItem = item.TipoItem,
                Nombre = item.Nombre,
                Precio = item.Precio,

                IdCategoria = item.Producto?.IdCategoria,
                StockMinimo = item.Producto?.StockMinimo
            };

            await CargarCategorias(modelo.IdCategoria);

            return View(modelo);
        }


        // ==========================================
        // 6. DESACTIVAR PRODUCTO O SERVICIO
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Desactivar(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            // Buscar el ítem en la base de datos.
            var item = await _context.ItemCatalogos
                .FirstOrDefaultAsync(i => i.IdItem == id);

            // Verificar si existe.
            if (item == null)
            {
                return NotFound();
            }

            // Evitar procesar nuevamente un ítem inactivo.
            if (!item.Estado)
            {
                TempData["Info"] =
                    "El ítem ya se encuentra desactivado.";

                return RedirectToAction(nameof(Index));
            }

            // Desactivación lógica:
            // no se elimina el registro de SQL Server.
            item.Estado = false;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "El ítem fue desactivado correctamente.";

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // 5. GUARDAR MODIFICACIONES
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(
            int id,
            EditarItemViewModel modelo)
        {
            // El identificador de la URL y del formulario
            // deben coincidir.
            if (id != modelo.IdItem)
            {
                return BadRequest();
            }

            var item = await _context.ItemCatalogos
                .Include(i => i.Producto)
                .FirstOrDefaultAsync(i =>
                    i.IdItem == id && i.Estado);

            if (item == null)
            {
                return NotFound();
            }

            if (item.TipoItem == "PRODUCTO" &&
                item.Producto == null)
            {
                return Problem(
                    "El producto no tiene datos de inventario."
                );
            }

            // Utilizamos el tipo real de la base de datos,
            // no un tipo enviado por el navegador.
            modelo.TipoItem = item.TipoItem;

            string nombre = modelo.Nombre?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ingrese un nombre válido."
                );
            }

            // Validaciones exclusivas de productos.
            if (item.TipoItem == "PRODUCTO")
            {
                if (!modelo.IdCategoria.HasValue ||
                    modelo.IdCategoria.Value <= 0)
                {
                    ModelState.AddModelError(
                        nameof(modelo.IdCategoria),
                        "Seleccione una categoría."
                    );
                }
                else
                {
                    bool categoriaExiste = await _context
                        .Set<Categorium>()
                        .AnyAsync(c =>
                            c.IdCategoria ==
                            modelo.IdCategoria.Value);

                    if (!categoriaExiste)
                    {
                        ModelState.AddModelError(
                            nameof(modelo.IdCategoria),
                            "La categoría no existe."
                        );
                    }
                }

                if (!modelo.StockMinimo.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(modelo.StockMinimo),
                        "Ingrese el stock mínimo."
                    );
                }
            }

            // Verificar duplicados, excluyendo
            // el registro que estamos editando.
            bool duplicado = await _context.ItemCatalogos
                .AnyAsync(i =>
                    i.IdItem != id &&
                    i.Nombre == nombre &&
                    i.TipoItem == item.TipoItem);

            if (duplicado)
            {
                ModelState.AddModelError(
                    nameof(modelo.Nombre),
                    "Ya existe otro ítem con ese nombre y tipo."
                );
            }

            if (!ModelState.IsValid)
            {
                await CargarCategorias(modelo.IdCategoria);
                return View(modelo);
            }

            // Actualizar datos generales.
            item.Nombre = nombre;
            item.Precio = modelo.Precio;

            // Actualizar solamente los datos permitidos
            // del producto. No modificar StockActual.
            if (item.TipoItem == "PRODUCTO")
            {
                item.Producto!.IdCategoria =
                    modelo.IdCategoria!.Value;

                item.Producto.StockMinimo =
                    modelo.StockMinimo!.Value;
            }

            try
            {
                await _context.SaveChangesAsync();

                TempData["Exito"] =
                    "El ítem fue actualizado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "No se pudieron guardar los cambios."
                );

                await CargarCategorias(modelo.IdCategoria);

                return View(modelo);
            }
        }

        // ==========================================
        // 6. CARGAR CATEGORÍAS
        // ==========================================

        private async Task CargarCategorias(
            int? categoriaSeleccionada = null)
        {
            var categorias = await _context
                .Set<Categorium>()
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.Categorias = new SelectList(
                categorias,
                "IdCategoria",
                "Nombre",
                categoriaSeleccionada
            );
        }
    }
}
