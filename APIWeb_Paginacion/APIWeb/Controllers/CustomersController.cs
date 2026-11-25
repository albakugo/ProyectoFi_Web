using APIWeb.Data;
using APIWeb.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Empleado")]
    public class CustomersController : ControllerBase
    {
        private readonly ContextoBD _context;

        public CustomersController(ContextoBD context)
        {
            _context = context;
        }

        // PROHIBIDO: GET /api/customers (SELECT * de todos los clientes)
        // Solo se permite busqueda exacta y con datos sensibles cubiertos
        [HttpPost("buscar")]
        public async Task<IActionResult> BuscarClientes([FromBody] BuscarClienteDto filtro)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { mensaje = "Ingresa un criterio de búsqueda válido (mínimo 2 caracteres)." });
            }

            string termino = filtro.Criterio.Trim().ToLower();

            // Limita resultados a maximo 15 coincidencias para evitar extraccion masiva
            var clientes = await _context.Customers
                .Where(c => c.Activo &&
                           (c.CustomerId.ToLower().Contains(termino) ||
                            c.CompanyName.ToLower().Contains(termino) ||
                            (c.ContactName != null && c.ContactName.ToLower().Contains(termino))))
                .Take(15)
                .ToListAsync();

            var idsClientes = clientes.Select(c => c.CustomerId).ToList();
            var correosPorCliente = await _context.Usuarios
                .Where(u => u.CustomerId != null && idsClientes.Contains(u.CustomerId))
                .ToDictionaryAsync(u => u.CustomerId!, u => u.Email);

            var resultado = clientes.Select(c =>
            {
                correosPorCliente.TryGetValue(c.CustomerId, out string? emailReal);

                return new ClienteConsultaDto
                {
                    CustomerId = c.CustomerId,
                    CompanyName = c.CompanyName,
                    ContactName = c.ContactName ?? string.Empty,
                    City = c.City,
                    Country = c.Country,
                    EmailEnmascarado = EnmascararEmail(emailReal),
                    TelefonoEnmascarado = EnmascararTelefono(c.Phone),
                    Activo = c.Activo
                };
            }).ToList();

            return Ok(resultado);
        }

        // Consulta exacta por ID 
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerClientePorId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 5)
            {
                return BadRequest(new { mensaje = "El identificador del cliente no es válido." });
            }

            var cliente = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id.ToUpper() && c.Activo);

            if (cliente == null)
            {
                return NotFound(new { mensaje = "No se encontró un cliente activo con ese identificador." });
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CustomerId == cliente.CustomerId);

            var dto = new ClienteConsultaDto
            {
                CustomerId = cliente.CustomerId,
                CompanyName = cliente.CompanyName,
                ContactName = cliente.ContactName ?? string.Empty,
                City = cliente.City,
                Country = cliente.Country,
                EmailEnmascarado = EnmascararEmail(usuario?.Email),
                TelefonoEnmascarado = EnmascararTelefono(cliente.Phone),
                Activo = cliente.Activo
            };

            return Ok(dto);
        }

        // Actualizacion segura de datos autorizados (no permite cambiar ID ni estado)
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCliente(string id, [FromBody] ActualizarClienteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { mensaje = "Los datos enviados no cumplen con el formato requerido." });
            }

            var cliente = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id.ToUpper() && c.Activo);

            if (cliente == null)
            {
                return NotFound(new { mensaje = "El cliente no existe o ya se encuentra dado de baja." });
            }

            // Sanitizacion contra etiquetas HTML/XSS antes de guardar
            cliente.CompanyName = LimpiarTexto(dto.CompanyName);
            cliente.ContactName = LimpiarTexto(dto.ContactName);
            cliente.City = string.IsNullOrWhiteSpace(dto.City) ? null : LimpiarTexto(dto.City);
            cliente.Country = string.IsNullOrWhiteSpace(dto.Country) ? null : LimpiarTexto(dto.Country);
            cliente.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : LimpiarTexto(dto.Phone);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Los datos del cliente se actualizaron correctamente." });
        }

        // Baja logica necesaria: NUNCA DELETE fisico y valida que no tenga compras en el carrito
        [HttpDelete("{id}")]
        public async Task<IActionResult> BajaLogicaCliente(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 5)
            {
                return BadRequest(new { mensaje = "El identificador del cliente no es válido." });
            }

            string customerId = id.ToUpper();

            var cliente = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cliente == null)
            {
                return NotFound(new { mensaje = "No se encontró el cliente especificado." });
            }

            if (!cliente.Activo)
            {
                return Conflict(new { mensaje = "Este cliente ya se encontraba dado de baja." });
            }

            // Regla de negocio: No se puede dar de baja un cliente si tiene compras en curso en el carrito
            bool tieneCarritoActivo = await _context.Orders
                .AnyAsync(o => o.CustomerId == customerId && o.Estado == "Carrito");

            if (tieneCarritoActivo)
            {
                return Conflict(new { mensaje = "No se puede dar de baja al cliente porque tiene productos pendientes en su carrito de compras." });
            }

            // Aplicar baja logica en Customers y en su cuenta de Usuarios asociada
            cliente.Activo = false;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CustomerId == customerId);

            if (usuario != null)
            {
                usuario.Activo = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "El cliente fue dado de baja correctamente." });
        }

        // --- Metodos privados de proteccion de datos ---

        private static string EnmascararEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return "Sin correo registrado";
            }

            var partes = email.Split('@');
            string usuario = partes[0];
            string dominio = partes[1];

            if (usuario.Length <= 2)
            {
                return $"{usuario[0]}***@{dominio}";
            }

            return $"{usuario.Substring(0, 2)}***@{dominio}";
        }

        private static string EnmascararTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return "Sin teléfono";
            }

            string limpio = telefono.Trim();
            if (limpio.Length <= 4)
            {
                return "****";
            }

            return $"{limpio.Substring(0, 3)}***{limpio.Substring(limpio.Length - 2)}";
        }

        private static string LimpiarTexto(string texto)
        {
            return texto.Replace("<", "").Replace(">", "").Trim();
        }
    }
}