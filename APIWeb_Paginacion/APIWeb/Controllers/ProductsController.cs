using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIWeb.Data;
using APIWeb.Models;
using APIWeb.Helpers;
using APIWeb.DTOs;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ContextoBD _context;

        public ProductsController(ContextoBD context)
        {
            _context = context;
        }

        // GET: api/Products (Para el CRUD de administración: devuelve todos los productos con DTO fijo)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetProducts()
        {
            return await _context.Products
                .Select(p => new ProductoResponseDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    UnitPrice = p.UnitPrice,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitsInStock = p.UnitsInStock,
                    Discontinued = p.Discontinued ?? false,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Sin categoría",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Sin proveedor"
                })
                .ToListAsync();
        }

        // GET: api/Products/activos (Para clientes y combos de ventas: solo Discontinued == false)
        [HttpGet("activos")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetProductsActivos()
        {
            return await _context.Products
                .Where(p => p.Discontinued == false)
                .Select(p => new ProductoResponseDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    UnitPrice = p.UnitPrice,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitsInStock = p.UnitsInStock,
                    Discontinued = false,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Sin categoría",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Sin proveedor"
                })
                .ToListAsync();
        }

        // GET: api/Products/BySupplier/5 (Solo productos activos del proveedor)
        [HttpGet("BySupplier/{supplierId}")]
        public async Task<ActionResult<IEnumerable<ProductoResponseDto>>> GetProductsBySupplier(int supplierId)
        {
            return await _context.Products
                .Where(p => p.SupplierId == supplierId && p.Discontinued == false)
                .Select(p => new ProductoResponseDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    UnitPrice = p.UnitPrice,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitsInStock = p.UnitsInStock,
                    Discontinued = false,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Sin categoría",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Sin proveedor"
                })
                .ToListAsync();
        }

        // GET: api/Products/paginado?busqueda=chai&pagina=1&tamanio=12
        // Vista de Cards para clientes: filtra obligatoriamente Discontinued == false
        [HttpGet("paginado")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetProductsPaginados(
            [FromQuery] string? busqueda = "",
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanio = 12)
        {
            if (pagina < 1) pagina = 1;
            if (tamanio < 1) tamanio = 12;
            if (tamanio > 48) tamanio = 48;

            // Filtro obligatorio para clientes: nunca mostrar descontinuados
            var query = _context.Products
                .Where(p => p.Discontinued == false)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                string filtro = busqueda.Trim().ToLower();
                query = query.Where(p =>
                    p.ProductName.ToLower().Contains(filtro) ||
                    (p.Category != null && p.Category.CategoryName.ToLower().Contains(filtro)) ||
                    (p.Supplier != null && p.Supplier.CompanyName.ToLower().Contains(filtro))
                );
            }

            int totalRegistros = await query.CountAsync();
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / tamanio);

            var productos = await query
                .OrderBy(p => p.ProductId)
                .Skip((pagina - 1) * tamanio)
                .Take(tamanio)
                .Select(p => new ProductoResponseDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    UnitPrice = p.UnitPrice,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitsInStock = p.UnitsInStock,
                    Discontinued = false,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Sin categoría",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Sin proveedor"
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

        // GET: api/Products/5 (Devuelve DTO fijo)
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductoResponseDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Where(p => p.ProductId == id)
                .Select(p => new ProductoResponseDto
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    UnitPrice = p.UnitPrice,
                    QuantityPerUnit = p.QuantityPerUnit,
                    UnitsInStock = p.UnitsInStock,
                    Discontinued = p.Discontinued ?? false,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Sin categoría",
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Sin proveedor"
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            return product;
        }

        // PUT: api/Products/5
        [Authorize(Roles = "Admin,Empleado")]
        // PUT: api/Products/5 (Recibe DTO fijo y sanitiza entrada)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromBody] ProductoRequestDto dto)
        {
            if (id != dto.ProductId)
            {
                return BadRequest(new { mensaje = "El ID de la URL no coincide con el del producto." });
            }

            string nombreLimpio;
            string? cantidadLimpia = null;

            try
            {
                nombreLimpio = SanitizadorHelper.ValidarYLimpiarTexto(dto.ProductName, "El nombre del producto");
                if (string.IsNullOrWhiteSpace(nombreLimpio))
                    return BadRequest(new { mensaje = "El nombre del producto es obligatorio." });

                if (!string.IsNullOrWhiteSpace(dto.QuantityPerUnit))
                {
                    cantidadLimpia = SanitizadorHelper.ValidarYLimpiarTexto(dto.QuantityPerUnit, "La cantidad por unidad");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            product.ProductName = nombreLimpio;
            product.SupplierId = dto.SupplierId;
            product.CategoryId = dto.CategoryId;
            product.QuantityPerUnit = cantidadLimpia;
            product.UnitPrice = dto.UnitPrice;
            product.UnitsInStock = dto.UnitsInStock;
            product.Discontinued = dto.Discontinued;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Products
        [Authorize(Roles = "Admin,Empleado")]
        // POST: api/Products (Recibe DTO fijo y sanitiza entrada)
        [HttpPost]
        public async Task<ActionResult<ProductoResponseDto>> PostProduct([FromBody] ProductoRequestDto dto)
        {
            string nombreLimpio;
            string? cantidadLimpia = null;

            try
            {
                nombreLimpio = SanitizadorHelper.ValidarYLimpiarTexto(dto.ProductName, "El nombre del producto");
                if (string.IsNullOrWhiteSpace(nombreLimpio))
                    return BadRequest(new { mensaje = "El nombre del producto es obligatorio." });

                if (!string.IsNullOrWhiteSpace(dto.QuantityPerUnit))
                {
                    cantidadLimpia = SanitizadorHelper.ValidarYLimpiarTexto(dto.QuantityPerUnit, "La cantidad por unidad");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }

            var product = new Product
            {
                ProductName = nombreLimpio,
                SupplierId = dto.SupplierId,
                CategoryId = dto.CategoryId,
                QuantityPerUnit = cantidadLimpia,
                UnitPrice = dto.UnitPrice,
                UnitsInStock = dto.UnitsInStock,
                Discontinued = dto.Discontinued
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var respuesta = new ProductoResponseDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                UnitPrice = product.UnitPrice,
                QuantityPerUnit = product.QuantityPerUnit,
                UnitsInStock = product.UnitsInStock,
                Discontinued = product.Discontinued ?? false,
                CategoryId = product.CategoryId,
                SupplierId = product.SupplierId
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, respuesta);
        }

        // DELETE: api/Products/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { mensaje = "Producto no encontrado." });
            }

            product.Discontinued = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}