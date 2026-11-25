using APIWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authorization;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ContextoBD _context;

        public SuppliersController(ContextoBD context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> ObtenerProveedores()
        {
            return await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.CompanyName
                })
                .ToListAsync();
        }
    }
}