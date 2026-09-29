using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [AuthorizeModule("POS")]
    public class CashRegisterController : ControllerBase
    {
        private readonly ICashRegisterService _service;
        private readonly IGlobalizationService _globalizationService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUser;

        public CashRegisterController(ICashRegisterService service, IGlobalizationService globalizationService, Onion.Common.Services.ICurrentUserService currentUser)
        {
            _service = service;
            _globalizationService = globalizationService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                if (pageNumber.HasValue)
                {
                    var paged = await _service.GetPagedAsync(pageNumber.Value, pageSize ?? 10);
                    return Ok(paged);
                }

                var items = await _service.GetAllAsync();
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
            if (item.RowVersion.Length > 0)
                Response.Headers.ETag = $"\"{Convert.ToBase64String(item.RowVersion)}\"";
            return Ok(item);
        }

        [HttpPost("open")]
        public async Task<IActionResult> Open([FromBody] CashRegister cash)
        {
            var created = await _service.OpenAsync(cash);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(int id, [FromBody] CloseCashRegisterRequest request)
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (current.RowVersion.Length > 0 && Request.Headers.TryGetValue("If-Match", out var ifMatch))
            {
                var token = ifMatch.ToString().Trim().Trim('"');
                if (!string.Equals(token, Convert.ToBase64String(current.RowVersion), StringComparison.Ordinal))
                    return StatusCode(StatusCodes.Status412PreconditionFailed, new { code = "CASH_REGISTER_CHANGED" });
            }
            await _service.CloseAsync(id, request.ActualAmount, request.BreakdownJson, _currentUser.UserId);
            return Ok(new { closed = true });
        }

        [HttpPut("{id}/pause")]
        public async Task<IActionResult> Pause(int id, [FromBody] PauseCashRegisterRequest request)
        {
            await _service.PauseAsync(id, request.Reason, _currentUser.UserId);
            return NoContent();
        }

        [HttpPut("{id}/resume")]
        public async Task<IActionResult> Resume(int id)
        {
            await _service.ResumeAsync(id, _currentUser.UserId);
            return NoContent();
        }

        public sealed record PauseCashRegisterRequest(string Reason);
        public sealed record CloseCashRegisterRequest(decimal ActualAmount, string? BreakdownJson);
    }
}
