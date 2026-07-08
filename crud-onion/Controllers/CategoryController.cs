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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CategoryDto categoryDto)
        {
            _logger.LogInformation("Create Category");
            await _categoryService.AddAsync(categoryDto);
            return Created(string.Empty, Onion.Common.Models.ApiResponse<object>.Ok(categoryDto, "Category created"));
        }
    }
}
