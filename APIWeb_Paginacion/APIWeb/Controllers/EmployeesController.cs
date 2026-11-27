using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;
using APIWeb.Data;
using APIWeb.DTOs;
using APIWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIWeb.Helpers;
using Microsoft.AspNetCore.Identity;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Exclusivo Administradores: empleados NO se registran
    public class EmployeesController : ControllerBase
    {
        private readonly ContextoBD _context;

        public EmployeesController(ContextoBD context)
        {
            _context = context;
        }

        // Consulta de empleados activos con DTO fijo (nunca viajan hashes ni datos internos)
        [HttpGet]
        public async Task<IActionResult> ObtenerEmpleados()
        {
            var empleados = await _context.Employees
                .Where(e => e.Activo)
                .ToListAsync();

            var idsEmpleados = empleados.Select(e => e.EmployeeId).ToList();
            var usuariosPorEmpleado = await _context.Usuarios
                .Where(u => u.EmployeeID != null && idsEmpleados.Contains(u.EmployeeID.Value))
                .ToDictionaryAsync(u => u.EmployeeID!.Value, u => u);

            var resultado = empleados.Select(e =>
            {
                usuariosPorEmpleado.TryGetValue(e.EmployeeId, out Usuario? usuario);

                return new EmpleadoConsultaDto
                {
                    EmployeeId = e.EmployeeId,
                    NombreCompleto = $"{e.FirstName} {e.LastName}",
                    Title = e.Title,
                    City = e.City,
                    NombreUsuario = usuario?.NombreUsuario ?? "Sin cuenta",
                    EmailEnmascarado = EnmascararEmail(usuario?.Email),
                    TelefonoEnmascarado = EnmascararTelefono(e.HomePhone),
                    Rol = usuario?.Rol ?? "Empleado",
                    Activo = e.Activo
                };
            }).ToList();

            return Ok(resultado);
        }

        // Alta de Empleado o Admin: verifica que no se repitan usuario, empleado ni cliente
        // y lo inserta en la tabla Employees y en Usuarios dentro de una transaccion
        [HttpPost]
        public async Task<IActionResult> RegistrarEmpleado([FromBody] RegistrarEmpleadoDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { mensaje = "Revisa los campos obligatorios y el formato de los datos." });
            }

            if (SanitizadorHelper.ContieneCodigoMalicioso(dto.NombreUsuario) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.Email) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.FirstName) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.LastName) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.Title) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.City) ||
                SanitizadorHelper.ContieneCodigoMalicioso(dto.HomePhone))
            {
                return BadRequest(new { mensaje = "Entrada rechazada: contiene etiquetas o código no permitido." });
            }

            string usuarioLimpio = LimpiarTexto(dto.NombreUsuario);
            string emailLimpio = LimpiarTexto(dto.Email).ToLower();
            string nombreLimpio = LimpiarTexto(dto.FirstName);
            string apellidoLimpio = LimpiarTexto(dto.LastName);

            // 1. Validar que no exista un usuario ni correo igual en Usuarios dentro de la bd
            bool existeUsuarioOEmail = await _context.Usuarios
                .AnyAsync(u => u.NombreUsuario.ToLower() == usuarioLimpio.ToLower() ||
                               u.Email.ToLower() == emailLimpio);

            if (existeUsuarioOEmail)
            {
                return Conflict(new { mensaje = "Ya existe una cuenta registrada con ese nombre de usuario o correo electrónico." });
            }

            // 2. Validar que no exista un empleado con el mismo nombre y apellido
            bool existeEmpleado = await _context.Employees
                .AnyAsync(e => e.FirstName.ToLower() == nombreLimpio.ToLower() &&
                               e.LastName.ToLower() == apellidoLimpio.ToLower());

            if (existeEmpleado)
            {
                return Conflict(new { mensaje = "Ya existe un empleado registrado con ese nombre y apellido." });
            }

            // 3. Validar que no exista un cliente con el mismo nombre de contacto o usuario igual
            string nombreCompletoNuevo = $"{nombreLimpio} {apellidoLimpio}".ToLower();
            bool existeClienteIgual = await _context.Customers
                .AnyAsync(c => c.CustomerId.ToLower() == usuarioLimpio.ToLower() ||
                               (c.ContactName != null && c.ContactName.ToLower() == nombreCompletoNuevo));

            if (existeClienteIgual)
            {
                return Conflict(new { mensaje = "No se puede registrar porque existe un cliente con datos idénticos en el sistema." });
            }

            // Transaccion para garantizar que se registre en Employees Y en Usuarios al mismo tiempo
            using var transaccion = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevoEmpleado = new Employee
                {
                    FirstName = nombreLimpio,
                    LastName = apellidoLimpio,
                    Title = string.IsNullOrWhiteSpace(dto.Title) ? dto.Rol : LimpiarTexto(dto.Title),
                    City = string.IsNullOrWhiteSpace(dto.City) ? null : LimpiarTexto(dto.City),
                    HomePhone = string.IsNullOrWhiteSpace(dto.HomePhone) ? null : LimpiarTexto(dto.HomePhone),
                    HireDate = DateTime.UtcNow,
                    Activo = true
                };

                _context.Employees.Add(nuevoEmpleado);
                await _context.SaveChangesAsync();

                var nuevoUsuario = new Usuario
                {
                    NombreUsuario = usuarioLimpio,
                    Email = emailLimpio,
                    Rol = dto.Rol, // "Admin" o "Empleado"
                    EmployeeID = nuevoEmpleado.EmployeeId,
                    CustomerID = null,
                    IntentosFallidos = 0,
                    Activo = true
                };

                var passwordHasher = new PasswordHasher<Usuario>();
                nuevoUsuario.PasswordHash = passwordHasher.HashPassword(nuevoUsuario, dto.Password);

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = $"El usuario con rol {dto.Rol} fue registrado exitosamente en empleados y acceso al sistema.",
                    employeeId = nuevoEmpleado.EmployeeId
                });
            }
            catch
            {
                await transaccion.RollbackAsync();
                return StatusCode(500, new { mensaje = "Ocurrió un problema interno al guardar el registro del empleado." });
            }
        }

        // Baja logica: NUNCA DELETE fisico y valida que NO tenga asociacion a compras (ordenes en Orders)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> BajaLogicaEmpleado(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { mensaje = "El identificador del empleado no es válido." });
            }

            var empleado = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (empleado == null)
            {
                return NotFound(new { mensaje = "No se encontró el empleado especificado." });
            }

            if (!empleado.Activo)
            {
                return Conflict(new { mensaje = "Este empleado ya se encontraba dado de baja." });
            }

            // Regla de negocio: No se puede dar de baja un empleado si tiene asociacion a compras
            bool tieneComprasAsociadas = await _context.Orders
                .AnyAsync(o => o.EmployeeId == id);

            if (tieneComprasAsociadas)
            {
                return Conflict(new { mensaje = "No se puede dar de baja al empleado porque tiene órdenes de compra asociadas en el historial." });
            }

            // Baja logica en Employees y desactivacion de cuenta en Usuarios
            empleado.Activo = false;

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.EmployeeID == id);

            if (usuario != null)
            {
                usuario.Activo = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "El empleado fue dado de baja correctamente." });
        }

        // --- Metodos privados auxiliares ---
        private static string EnmascararEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return "Sin correo";
            }

            var partes = email.Split('@');
            string usuario = partes[0];
            string dominio = partes[1];

            return usuario.Length <= 2
                ? $"{usuario[0]}***@{dominio}"
                : $"{usuario.Substring(0, 2)}***@{dominio}";
        }

        private static string EnmascararTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return "Sin teléfono";
            }

            string limpio = telefono.Trim();
            return limpio.Length <= 4
                ? "****"
                : $"{limpio.Substring(0, 3)}***{limpio.Substring(limpio.Length - 2)}";
        }

        private static string LimpiarTexto(string texto)
        {
            return texto.Replace("<", "").Replace(">", "").Trim();
        }
    }
}