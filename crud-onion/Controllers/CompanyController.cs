using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _service;
        private readonly IUserService _userService;
        private readonly IGlobalizationService _globalizationService;

        public CompanyController(ICompanyService service, IUserService userService, IGlobalizationService globalizationService)
        {
            _service = service;
            _userService = userService;
            _globalizationService = globalizationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _service.GetAllAsync();
            if (!items.Any())
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.CompaniesNotFound) },
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            try
            {
                return Ok(Onion.Common.Models.ApiResponse<object>.Ok(items));
            }
            catch (System.Exception)
            {
                throw new HttpResponseException
                {
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.UnknownException) },
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(Onion.Common.Models.ApiResponse<object>.Fail("Company not found"));
            return Ok(Onion.Common.Models.ApiResponse<object>.Ok(item));
        }

        // Create company and assign the creating user as owner
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Onion.BussinesLogic.Dtos.CreateCompanyRequest request)
        {
            var created = await _service.CreateAsync(request);

            // If user is authenticated, associate user to the created company
            var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                await _userService.AssignCompanyAsync(userId, created.Id);
            }

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Company company)
        {
            await _service.UpdateAsync(company);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
