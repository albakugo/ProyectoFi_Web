using APIWeb.Data;
using APIWeb.DTOs;
using APIWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

using APIWeb.Helpers;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ContextoBD _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        //iniciamos con el captcha
        private readonly IMemoryCache _cache;


        public AuthController(
            ContextoBD context,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            //permite trabajar recibir: BD, config de la API, contra protegidas
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Usuario>();
            _cache = cache;
        }

        //crea ruta para iniciar sesion
        [HttpPost("login")]
        //recibira usu y pass enviados del HTML
        public async Task<IActionResult> Login(LoginRequest request)
        {

            // VALIDAR CAPTCHA ANTES DE INICIAR SESION

            string claveCaptcha = "Captcha_" + request.CaptchaId;

            // Buscar la respuesta correcta guardada
            if (!_cache.TryGetValue(claveCaptcha, out string? respuestaCorrecta))
            {
                return BadRequest(new
                {
                    mensaje = "El CAPTCHA expiró o no existe. Genera uno nuevo."
                });
            }

            // Eliminar el CAPTCHA para evitar que se reutilice
            _cache.Remove(claveCaptcha);

            // Comparar respuesta
            if (request.RespuestaCaptcha != respuestaCorrecta)
            {
                return BadRequest(new
                {
                    mensaje = "La respuesta del CAPTCHA es incorrecta."
                });
            }


            //busca al usuario en BD con gmail, nom o correo
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
                u.NombreUsuario == request.Usuario ||
                u.Email == request.Usuario);

            //si encontro
            if (usuario == null)
            {
                return Unauthorized(new
                {
                    mensaje = "Usuario o contraseña incorrectos."
                });
            }
            // si esta activo 
            if (!usuario.Activo)
            {
                return Unauthorized(new
                {
                    mensaje = "La cuenta está desactivada."
                });
            }
            //si esta bloqueado, si hay fecha de bloqueo?
            if (usuario.BloqueadoHasta.HasValue &&
                usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                // Calcula cuánto tiempo falta para desbloquear la cuenta
                var tiempoRestante = usuario.BloqueadoHasta.Value - DateTime.UtcNow;

                return Unauthorized(new
                {
                    mensaje = $"La cuenta está bloqueada. Intenta nuevamente en {Math.Ceiling(tiempoRestante.TotalMinutes)} minuto(s)."
                });
            }

            //comprueba pass con BD
            // Comprueba contraseña con BD
            PasswordVerificationResult resultado = PasswordVerificationResult.Failed;

            try
            {
                resultado = _passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    request.Password
                );
            }
            catch (FormatException)
            {
                resultado = PasswordVerificationResult.Failed;
            }

            //si la contraseña es antigua, la convierte a hash
            if (resultado == PasswordVerificationResult.Failed &&
                string.Equals(usuario.PasswordHash, request.Password, StringComparison.Ordinal))
            {
                usuario.PasswordHash = _passwordHasher.HashPassword(
                    usuario,
                    request.Password
                );

                resultado = PasswordVerificationResult.Success;
            }

            //si no coincide, inica los intentos
            if (resultado == PasswordVerificationResult.Failed)
            {
                //incrementa y llega a 3
                usuario.IntentosFallidos++;

                if (usuario.IntentosFallidos >= 3)
                {
                    //y bloquea 15 min
                    usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                    usuario.IntentosFallidos = 0;
                }

                //guarda en BD loscambios
                await _context.SaveChangesAsync();

                return Unauthorized(new
                {
                    mensaje = "Usuario o contraseña incorrectos."
                });
            }

            //si es correcto, reinicia contador, elimina fecha de bloque y guarda cambios
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;

            await _context.SaveChangesAsync();

            //si es correcto, reinicia contador, elimina fecha de bloque y guarda cambios
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;

            await _context.SaveChangesAsync();

            //datos obtenidos de .json
            var clave = _configuration["Jwt:Key"]!;
            var emisor = _configuration["Jwt:Issuer"];
            var audiencia = _configuration["Jwt:Audience"];

            //datos del token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    usuario.UsuarioId.ToString()),

                new Claim(ClaimTypes.Name,
                    usuario.NombreUsuario),

                new Claim(ClaimTypes.Role,
                    usuario.Rol)
            };

            //crea clave para token
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(clave)
            );

            //indica usar SHA-256 para token
            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            //construye token con emisor, audiencia, info de usuario, tiempoexpiracion
            var token = new JwtSecurityToken(
                issuer: emisor,
                audience: audiencia,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return Ok(new
            {
                mensaje = "Inicio de sesión correcto.",
                token = new JwtSecurityTokenHandler().WriteToken(token),
                usuario = usuario.NombreUsuario,
                rol = usuario.Rol
            });
        }



        //registro de clientes
        [HttpPost("registro-cliente")]
        public async Task<IActionResult> RegistroCliente(RegistroClienteRequest request)
        {
            string nombreUsuario = request.NombreUsuario.Trim();

            if (SanitizadorHelper.ContieneCodigoMalicioso(request.NombreUsuario) ||
                SanitizadorHelper.ContieneCodigoMalicioso(request.Email) ||
                SanitizadorHelper.ContieneCodigoMalicioso(request.Nombre) ||
                SanitizadorHelper.ContieneCodigoMalicioso(request.Empresa) ||
                SanitizadorHelper.ContieneCodigoMalicioso(request.Telefono))
            {
                return BadRequest(new { mensaje = "Los datos de registro contienen caracteres o código no permitido." });
            }

            string email = request.Email.Trim();
            string nombre = request.Nombre.Trim();
            string empresa = request.Empresa.Trim();

            // Verificar que el usuario o correo no existan
            bool existe = await _context.Usuarios.AnyAsync(u =>
                u.NombreUsuario == nombreUsuario ||
                u.Email == email);

            if (existe)
            {
                return Conflict(new
                {
                    mensaje = "El nombre de usuario o correo ya está registrado."
                });
            }

            // Generar un CustomerID de cinco caracteres disponible
            string customerId;

            do
            {
                const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                char[] codigo = new char[5];

                codigo[0] = 'C';

                for (int i = 1; i < 5; i++)
                {
                    codigo[i] = caracteres[
                        RandomNumberGenerator.GetInt32(caracteres.Length)
                    ];
                }

                customerId = new string(codigo);

            } while (await _context.Customers.AnyAsync(c =>
                c.CustomerId == customerId));

            // Crear el cliente
            var cliente = new Customer
            {
                CustomerId = customerId,
                CompanyName = empresa,
                ContactName = nombre,
                Phone = request.Telefono?.Trim()
            };

            // Crear el usuario
            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Email = email,
                Nombre = nombre,
                Rol = "Cliente",
                CustomerID = customerId,
                Activo = true,
                IntentosFallidos = 0,
                BloqueadoHasta = null
            };

            // Proteger la contraseña mediante hash
            usuario.PasswordHash = _passwordHasher.HashPassword(
                usuario,
                request.Password
            );

            // Guardar ambos registros en una transacción
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Customers.Add(cliente);
                _context.Usuarios.Add(usuario);

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = "Registro de cliente exitoso.",
                    usuario = usuario.NombreUsuario,
                    rol = usuario.Rol,
                    customerId = customerId
                });
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();

                return Conflict(new
                {
                    mensaje = "No fue posible completar el registro. Verifica que los datos no estén duplicados."
                });
            }
        }





        [HttpGet("captcha")]
        public IActionResult GenerarCaptcha()
        {
            //generar num aleatorio
            int numero1 = RandomNumberGenerator.GetInt32(1, 10);
            int numero2 = RandomNumberGenerator.GetInt32(1, 10);

            //junta 2 num 
            string pregunta = $"{numero1} + {numero2}";
            int respuesta = numero1 + numero2;

            //crear id uni para la pregunta
            string captchaId = Guid.NewGuid().ToString();

            //guarda respuesta por 2 min
            _cache.Set(
                "Captcha_" + captchaId,
                respuesta.ToString(),
                TimeSpan.FromMinutes(2)
            );

            //envia al navegador pregunta y el id 
            return Ok(new
            {
                captchaId = captchaId,
                pregunta = pregunta
            });
        }


        [HttpPost("validar-captcha")]
        public IActionResult ValidarCaptcha(CaptchaRequest request)
        {
            // Buscar la respuesta guardada
            string clave = "Captcha_" + request.CaptchaId;

            if (!_cache.TryGetValue(clave, out string? respuestaCorrecta))
            {
                return BadRequest(new
                {
                    mensaje = "El CAPTCHA expiró o no existe."
                });
            }

            // Eliminar el CAPTCHA para que no se reutilice
            _cache.Remove(clave);

            // Comparar la respuesta del usuario
            if (request.Respuesta != respuestaCorrecta)
            {
                return BadRequest(new
                {
                    mensaje = "La respuesta del CAPTCHA es incorrecta."
                });
            }

            return Ok(new
            {
                mensaje = "CAPTCHA correcto."
            });
        }


    }
}