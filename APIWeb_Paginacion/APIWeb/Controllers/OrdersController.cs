using APIWeb.Data;
using APIWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Cliente")]
    public class OrdersController : ControllerBase
    {
        private readonly ContextoBD _context;

        public OrdersController(ContextoBD context)
        {
            _context = context;
        }

        // Lo único que manda la página: producto y cantidad
        public class AgregarCarritoRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
        }

        // El CustomerId sale del TOKEN, nunca de la página
        private string? ObtenerCustomerId()
        {
            return User.FindFirst("CustomerId")?.Value;
        }

        // GET: api/Orders/carrito
        [HttpGet("carrito")]
        public async Task<IActionResult> VerCarrito()
        {
            var customerId = "test";//ObtenerCustomerId();
            if (string.IsNullOrEmpty(customerId))
                return BadRequest(new { mensaje = "Tu token no tiene CustomerId." });

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (carrito == null)
                return Ok(new { items = new List<object>(), total = 0m });

            var items = carrito.OrderDetails.Select(d => new
            {
                productId = d.ProductId,
                productName = d.Product.ProductName,
                unitPrice = d.UnitPrice,
                quantity = d.Quantity,
                subtotal = d.UnitPrice * d.Quantity
            }).ToList();

            decimal total = items.Sum(i => i.subtotal);   // total calculado en el servidor

            return Ok(new { items, total });
        }

        // POST: api/Orders/carrito/agregar
        [HttpPost("carrito/agregar")]
        public async Task<IActionResult> AgregarAlCarrito([FromBody] AgregarCarritoRequest dto)
        {
            var customerId = ObtenerCustomerId();
            if (string.IsNullOrEmpty(customerId))
                return BadRequest(new { mensaje = "Tu token no tiene CustomerId." });

            if (dto.Quantity <= 0)
                return BadRequest(new { mensaje = "La cantidad debe ser mayor a 0." });

            // Se consulta el producto en la BD (no se confía en la página)
            var producto = await _context.Products.FindAsync(dto.ProductId);
            if (producto == null || producto.Discontinued != false)
                return BadRequest(new { mensaje = "El producto no existe o está descontinuado." });

            int stock = producto.UnitsInStock ?? 0;
            if (stock <= 0)
                return BadRequest(new { mensaje = "El producto no tiene stock disponible." });

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            OrderDetail? detalle = null;
            int cantidadEnCarrito = 0;
            if (carrito != null)
            {
                detalle = carrito.OrderDetails.FirstOrDefault(d => d.ProductId == dto.ProductId);
                cantidadEnCarrito = detalle?.Quantity ?? 0;
            }

            long cantidadNueva = (long)cantidadEnCarrito + dto.Quantity;
            if (cantidadNueva > stock)
                return BadRequest(new { mensaje = $"Stock insuficiente. Disponible: {stock}. Ya tienes {cantidadEnCarrito} en tu carrito." });

            if (carrito == null)
            {
                carrito = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Estado = "Carrito"
                };
                _context.Orders.Add(carrito);
            }

            if (detalle == null)
            {
                carrito.OrderDetails.Add(new OrderDetail
                {
                    ProductId = producto.ProductId,
                    UnitPrice = producto.UnitPrice ?? 0m,   // precio real de la BD
                    Quantity = dto.Quantity,
                    Discount = 0
                });
            }
            else
            {
                detalle.Quantity = (int)cantidadNueva;
                detalle.UnitPrice = producto.UnitPrice ?? 0m;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Se agregó '{producto.ProductName}' al carrito." });
        }

        // POST: api/Orders/comprar
        [HttpPost("comprar")]
        public async Task<IActionResult> Comprar()
        {
            var customerId = ObtenerCustomerId();
            if (string.IsNullOrEmpty(customerId))
                return BadRequest(new { mensaje = "Tu token no tiene CustomerId." });

            await using var transaccion = await _context.Database.BeginTransactionAsync();

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (carrito == null || carrito.OrderDetails.Count == 0)
                return BadRequest(new { mensaje = "Tu carrito está vacío." });

            foreach (var d in carrito.OrderDetails)
            {
                if (d.Product.Discontinued != false)
                    return BadRequest(new { mensaje = $"'{d.Product.ProductName}' ya no está disponible." });

                int stock = d.Product.UnitsInStock ?? 0;
                if (d.Quantity > stock)
                    return BadRequest(new { mensaje = $"Stock insuficiente de '{d.Product.ProductName}'. Disponible: {stock}." });
            }

            decimal total = 0m;
            foreach (var d in carrito.OrderDetails)
            {
                d.Product.UnitsInStock = (d.Product.UnitsInStock ?? 0) - d.Quantity;
                total += d.UnitPrice * d.Quantity;
            }

            carrito.Estado = "Comprado";
            carrito.OrderDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Ok(new { mensaje = "¡Compra realizada con éxito!", orderId = carrito.OrderId, total });
        }
    }
}