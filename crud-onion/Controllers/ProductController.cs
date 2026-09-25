using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.DataAccess.Models;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
            : base()
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices()
        {
            var services = await _productService.GetByTypeAsync(2);
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(services));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("Get Product By Id: {Id}", id);

            var product = await _productService.GetAsync(id);

            if (product is null)
                return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("El producto no existe."));

            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(product));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string description)
        {
            _logger.LogInformation("Search Products by Description: {Description}", description);

            var products = await _productService.SearchByDescriptionAsync(description);

            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(products));
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] FilterPayload filterPayload,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Get Paged Products");

            var result = await _productService.GetPagedListAsync(filterPayload, ct);

            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProductDto productDto)
        {
            _logger.LogInformation("Create Product");
            try
            {
                await _productService.AddAsync(productDto);
                return Created(string.Empty, Onion.Common.Models.ApiResponse<object>.Ok(productDto, "Product created"));
            }
            catch (CustomException ex)
            {
                return MapCustomExceptionToActionResult(ex);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] ProductDto productDto)
        {
            _logger.LogInformation("Update Product");
            try
            {
                await _productService.UpdateAsync(productDto);
                return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Product updated"));
            }
            catch (CustomException ex)
            {
                return MapCustomExceptionToActionResult(ex);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Delete Product");
            try
            {
                await _productService.DeleteAsync(id);
                return Ok(Onion.Common.Models.ApiResponse<object>.Ok(null, "Product deleted"));
            }
            catch (CustomException ex)
            {
                return MapCustomExceptionToActionResult(ex);
            }
        }

        private IActionResult MapCustomExceptionToActionResult(CustomException ex)
        {
            var code = ex.Error?.Code ?? string.Empty;
            var message = ex.Error?.Message ?? ex.Message;
            // Log the mapped error to help debugging from server side
            _logger?.LogWarning("Product operation failed: {Code} - {Message}", code, message);

            switch (code)
            {
                case "DUPLICATE_BARCODE":
                case "DUPLICATE_NAME":
                    return Conflict(Onion.Common.Models.ApiResponse<object>.Fail(message));
                case "NOT_FOUND":
                    return NotFound(Onion.Common.Models.ApiResponse<object>.Fail(message));
                case "PLAN_LIMIT":
                    return StatusCode(403, Onion.Common.Models.ApiResponse<object>.Fail(message));
                case "DB_ERROR":
                    return StatusCode(500, Onion.Common.Models.ApiResponse<object>.Fail(message));
                case "INVALID_NAME":
                case "INVALID_COST":
                case "INVALID_STOCK":
                case "COMPANY_REQUIRED":
                case "FK_NOT_FOUND":
                default:
                    return BadRequest(Onion.Common.Models.ApiResponse<object>.Fail(message));
            }
        }
    }
}