using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIWeb.Data;
using APIWeb.Models;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ContextoBD _context;

        public OrdersController(ContextoBD context)
        {
            _context = context;
        }

        public class AgregarCarritoDto
        {
            public int ProductId { get; set; }
            public short Quantity { get; set; }
        }

        private async Task<string?> ObtenerCustomerIdUsuarioAsync()
        {
            var claimCustomer = User.FindFirst("CustomerId")?.Value
                             ?? User.FindFirst("CustomerID")?.Value;
            if (!string.IsNullOrWhiteSpace(claimCustomer))
                return claimCustomer;

            var nombreUsuario = User.Identity?.Name
                             ?? User.FindFirst(ClaimTypes.Name)?.Value
                             ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return null;

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario || u.Email == nombreUsuario);

            return usuario?.CustomerId;
        }

        // GET: api/Orders/carrito
        [HttpGet("carrito")]
        public async Task<ActionResult<object>> GetCarrito()
        {
            var customerId = await ObtenerCustomerIdUsuarioAsync();
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { mensaje = "El usuario autenticado no tiene un CustomerID asociado." });

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (carrito == null || !carrito.OrderDetails.Any())
            {
                return Ok(new
                {
                    items = Array.Empty<object>(),
                    total = 0m
                });
            }

            var items = carrito.OrderDetails.Select(od => new
            {
                od.ProductId,
                ProductName = od.Product != null ? od.Product.ProductName : $"Producto #{od.ProductId}",
                od.UnitPrice,
                od.Quantity,
                Subtotal = od.UnitPrice * od.Quantity
            }).ToList();

            decimal total = items.Sum(i => i.Subtotal);

            return Ok(new
            {
                orderId = carrito.OrderId,
                items,
                total
            });
        }

        // POST: api/Orders/carrito/agregar
        [HttpPost("carrito/agregar")]
        public async Task<ActionResult<object>> AgregarAlCarrito([FromBody] AgregarCarritoDto dto)
        {
            if (dto == null || dto.ProductId <= 0 || dto.Quantity <= 0)
                return BadRequest(new { mensaje = "Producto o cantidad inválidos." });

            var customerId = await ObtenerCustomerIdUsuarioAsync();
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { mensaje = "El usuario autenticado no tiene un CustomerID asociado." });

            var producto = await _context.Products.FindAsync(dto.ProductId);
            if (producto == null)
                return NotFound(new { mensaje = "El producto no existe." });

            if (producto.Discontinued == true)
                return BadRequest(new { mensaje = "No se puede agregar un producto descontinuado." });

            int stockDisponible = producto.UnitsInStock ?? 0;
            if (stockDisponible <= 0)
                return BadRequest(new { mensaje = "El producto no tiene stock disponible." });

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (carrito == null)
            {
                carrito = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Estado = "Carrito"
                };
                _context.Orders.Add(carrito);
                await _context.SaveChangesAsync();
            }

            var detalleExistente = carrito.OrderDetails
                .FirstOrDefault(od => od.ProductId == dto.ProductId);

            int cantidadTotalDeseada = (detalleExistente?.Quantity ?? 0) + dto.Quantity;
            if (cantidadTotalDeseada > stockDisponible)
                return BadRequest(new { mensaje = $"Stock insuficiente. Disponible: {stockDisponible} uds." });

            if (detalleExistente != null)
            {
                detalleExistente.Quantity = (short)cantidadTotalDeseada;
                detalleExistente.UnitPrice = producto.UnitPrice ?? 0m;
            }
            else
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = carrito.OrderId,
                    ProductId = producto.ProductId,
                    UnitPrice = producto.UnitPrice ?? 0m,
                    Quantity = dto.Quantity,
                    Discount = 0f
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = $"Se agregó '{producto.ProductName}' al carrito." });
        }

        // POST: api/Orders/comprar
        [HttpPost("comprar")]
        public async Task<ActionResult<object>> ConfirmarCompra()
        {
            var customerId = await ObtenerCustomerIdUsuarioAsync();
            if (string.IsNullOrWhiteSpace(customerId))
                return BadRequest(new { mensaje = "El usuario autenticado no tiene un CustomerID asociado." });

            var carrito = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (carrito == null || !carrito.OrderDetails.Any())
                return BadRequest(new { mensaje = "Tu carrito está vacío." });

            foreach (var detalle in carrito.OrderDetails)
            {
                if (detalle.Product == null || detalle.Product.Discontinued == true)
                    return BadRequest(new { mensaje = $"El producto #{detalle.ProductId} ya no está disponible." });

                int stock = detalle.Product.UnitsInStock ?? 0;
                if (detalle.Quantity > stock)
                    return BadRequest(new { mensaje = $"Stock insuficiente para '{detalle.Product.ProductName}' (disponible: {stock})." });
            }

            decimal total = 0m;
            foreach (var detalle in carrito.OrderDetails)
            {
                detalle.Product!.UnitsInStock = (short)((detalle.Product.UnitsInStock ?? 0) - detalle.Quantity);
                total += detalle.UnitPrice * detalle.Quantity;
            }

            carrito.OrderDate = DateTime.Now;
            carrito.Estado = "Comprado";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "¡Compra confirmada con éxito!",
                orderId = carrito.OrderId,
                total
            });
        }
    }
}