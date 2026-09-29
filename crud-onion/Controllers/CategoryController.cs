using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onion.BusinessLogic.Dtos;
using Onion.BusinessLogic.Services.Abstract;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _categoryService.GetAllAsync();
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(items));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _categoryService.GetByIdAsync(id);
            if (item == null) return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("Category not found"));
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(item));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CategoryDto categoryDto)
        {
            _logger.LogInformation("Create Category");
            await _categoryService.AddAsync(categoryDto);
            return Created(string.Empty, Onion.Common.Models.ApiResponse<object>.Ok(categoryDto, "Category created"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete Category {Id}", id);
            await _categoryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
