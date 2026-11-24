using APIWeb.Data;
using APIWeb.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authorization;
using System;

namespace APIWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ContextoBD _context;

        public CategoriesController(ContextoBD context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Object>>> ObtenerCategorias()
        {
            return await _context.Categories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                }).ToListAsync();
        }

        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<Category>>> ObtenerCategoriasSimplificadas()
        //{
        //    return await _context.Categories.ToListAsync<Category>();
        //}
    }
}
