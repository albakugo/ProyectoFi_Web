using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using APIWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace APIWeb.Controllers
{
    // SOLO PARA PRUEBAS
    [Route("api/[controller]")]
    [ApiController]
    public class TokenPruebaController : ControllerBase
    {
        private readonly ContextoBD _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public TokenPruebaController(ContextoBD context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        // GET: api/TokenPrueba?customerId=ALFKI
        [HttpGet]
        public async Task<IActionResult> Generar(string customerId = "ALFKI")
        {
            
            if (!_env.IsDevelopment()) return NotFound();

            bool existe = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!existe)
                return BadRequest(new { mensaje = "Ese cliente no existe en la tabla Customers." });

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "usuario-prueba"),
                new Claim(ClaimTypes.Role, "Cliente"),
                new Claim("CustomerId", customerId)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: credenciales
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}