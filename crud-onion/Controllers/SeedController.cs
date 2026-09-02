using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Products;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SeedController> _logger;

        public SeedController(IUnitOfWork uow, ILogger<SeedController> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        [HttpPost("setup")]
        public async Task<IActionResult> SetupDefaults()
        {
            // Seed product types if none
            var ptList = (await _uow.ProductTypes.ListAsync()).ToList();
            if (!ptList.Any())
            {
                var defaults = new List<ProductType>
                {
                    new ProductType { Description = "Producto Estándar" },
                    new ProductType { Description = "Servicio" },
                    new ProductType { Description = "Digital" },
                    new ProductType { Description = "Combo" },
                    new ProductType { Description = "Materia Prima" }
                };
                foreach (var d in defaults) await _uow.ProductTypes.AddAsync(d);
            }

            // Seed categories if none
            var catList = (await _uow.Categories.ListAsync()).ToList();
            if (!catList.Any())
            {
                var cats = new List<Onion.Domain.Products.Category>
                {
                    new Onion.Domain.Products.Category { Description = "General" },
                    new Onion.Domain.Products.Category { Description = "Alimentos" },
                    new Onion.Domain.Products.Category { Description = "Bebidas" },
                    new Onion.Domain.Products.Category { Description = "Papelería" },
                    new Onion.Domain.Products.Category { Description = "Servicios" }
                };
                foreach (var c in cats) await _uow.Categories.AddAsync(c);
            }

            await _uow.SaveChangesAsync();
            _logger.LogInformation("Seed setup completed");
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Seed data inserted if missing"));
        }
    }
}
