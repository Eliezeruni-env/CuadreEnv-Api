using Microsoft.AspNetCore.Mvc;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CashMovementController : ControllerBase
    {
        private readonly ICashMovementService _service;
        private readonly IGlobalizationService _globalizationService;

        public CashMovementController(ICashMovementService service, IGlobalizationService globalizationService)
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
                    Errors = new Onion.Common.Models.Error[] { _globalizationService.GetErrorInCurrentLanguage(ErrorCodes.CashMovementsNotFound) },
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CashMovement cm)
        {
            var created = await _service.AddAsync(cm);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }
    }
}
