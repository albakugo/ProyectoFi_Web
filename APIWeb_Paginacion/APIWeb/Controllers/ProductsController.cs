using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIWeb.Data;
using APIWeb.Models;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ContextoBD _context;

        public ProductsController(ContextoBD context)
        {
            _context = context;
        }

        // GET: api/Products (Devuelve todos los productos para el CRUD sin afectarlo)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            return await _context.Products
                .AsNoTracking()
                .Select(p => new
                {
                    p.ProductId,
                    ProductName = p.ProductName ?? "Sin nombre",
                    p.UnitPrice,
                    p.QuantityPerUnit,
                    p.UnitsInStock,
                    Discontinued = p.Discontinued ?? false,
                    p.CategoryId,
                    p.SupplierId,
                    CategoryName = p.Category != null && p.Category.CategoryName != null
                        ? p.Category.CategoryName
                        : "Sin categoría",
                    SupplierName = p.Supplier != null && p.Supplier.CompanyName != null
                        ? p.Supplier.CompanyName
                        : "Sin proveedor"
                })
                .ToListAsync();
        }

        // Endpoint opcional si ya usabas filtro por proveedor en el CRUD
        [HttpGet("BySupplier/{supplierId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductsBySupplier(int supplierId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => p.SupplierId == supplierId)
                .Select(p => new
                {
                    p.ProductId,
                    ProductName = p.ProductName ?? "Sin nombre",
                    p.UnitPrice,
                    p.QuantityPerUnit,
                    p.UnitsInStock,
                    Discontinued = p.Discontinued ?? false,
                    CategoryName = p.Category != null && p.Category.CategoryName != null
                        ? p.Category.CategoryName
                        : "Sin categoría",
                    SupplierName = p.Supplier != null && p.Supplier.CompanyName != null
                        ? p.Supplier.CompanyName
                        : "Sin proveedor"
                })
                .ToListAsync();
        }

        // GET: api/Products/paginado?busqueda=chai&pagina=1&tamanio=12
        // Endpoint alterno exclusivo para filtrar y paginar en la vista de Cards
        [HttpGet("paginado")]
        public async Task<ActionResult<object>> GetProductsPaginados(
            [FromQuery] string? busqueda = "",
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanio = 12)
        {
            try
            {
                if (pagina < 1) pagina = 1;
                if (tamanio < 1) tamanio = 12;
                if (tamanio > 48) tamanio = 48; // Validar maximo permitido

                var query = _context.Products.AsNoTracking().AsQueryable();

                // Aplicar filtro si el usuario escribio algo en el cuadro de texto
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    string patron = $"%{busqueda.Trim()}%";
                    query = query.Where(p =>
                        EF.Functions.Like(p.ProductName ?? "", patron) ||
                        (p.Category != null && EF.Functions.Like(p.Category.CategoryName ?? "", patron)) ||
                        (p.Supplier != null && EF.Functions.Like(p.Supplier.CompanyName ?? "", patron))
                    );
                }

                int totalRegistros = await query.CountAsync();
                int totalPaginas = totalRegistros == 0 ? 0 : (int)Math.Ceiling((double)totalRegistros / tamanio);

                var productos = await query
                    .OrderBy(p => p.ProductId) // Orden determinista para evitar duplicados o saltos entre paginas
                    .Skip((pagina - 1) * tamanio)
                    .Take(tamanio)
                    .Select(p => new
                    {
                        p.ProductId,
                        ProductName = p.ProductName ?? "Sin nombre",
                        p.UnitPrice,
                        p.QuantityPerUnit,
                        p.UnitsInStock,
                        Discontinued = p.Discontinued ?? false,
                        CategoryName = p.Category != null && p.Category.CategoryName != null
                            ? p.Category.CategoryName
                            : "Sin categoría",
                        SupplierName = p.Supplier != null && p.Supplier.CompanyName != null
                            ? p.Supplier.CompanyName
                            : "Sin proveedor"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    paginaActual = pagina,
                    tamanioPagina = tamanio,
                    totalRegistros,
                    totalPaginas,
                    hayMas = pagina < totalPaginas,
                    datos = productos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "Error interno al obtener los productos paginados.",
                    detalle = ex.Message
                });
            }
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}