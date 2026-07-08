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
    public class CashRegisterController : ControllerBase
    {
        private readonly ICashRegisterService _service;
        private readonly IGlobalizationService _globalizationService;

        public CashRegisterController(ICashRegisterService service, IGlobalizationService globalizationService)
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
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.CashRegistersNotFound) },
                    StatusCode = System.Net.HttpStatusCode.BadRequest
                };
            }
            try
            {
                return Ok(await _service.GetAllAsync());
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
            return Ok(item);
        }

        [HttpPost("open")]
        public async Task<IActionResult> Open([FromBody] CashRegister cash)
        {
            var created = await _service.OpenAsync(cash);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(int id, [FromQuery] decimal closingAmount)
        {
            await _service.CloseAsync(id, closingAmount);
            return NoContent();
        }
    }
}
