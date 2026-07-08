using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain.Users;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly IGlobalizationService _globalizationService;

        public UserController(IUserService service, IGlobalizationService globalizationService)
        {
            _service = service;
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
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.UsersNotFound) },
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            try
            {
                return Ok(items);
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
            if (item == null) return NotFound();
            item.PasswordHash = string.Empty;
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            var created = await _service.CreateAsync(user);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, new { created.Id, created.Email });
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] User user)
        {
            await _service.UpdateAsync(user);
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
