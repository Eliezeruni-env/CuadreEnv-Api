using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;
using Onion.BussinesLogic.Services.Abstract;

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
            var companyId = _currentUser.CompanyId;
            if (!companyId.HasValue || companyId.Value <= 0)
                return BadRequest(new { code = "COMPANY_REQUIRED", message = "El usuario autenticado no tiene una empresa asignada." });

            cash.CompanyId = companyId.Value;
            cash.OpenedByUserId ??= _currentUser.UserId;

            var created = await _service.OpenAsync(cash);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CloseCashRegisterRequest? request)
        {
            request ??= new CloseCashRegisterRequest();
            if (request.ActualAmount == 0 && Request.Query.TryGetValue("closingAmount", out var qAmount) && decimal.TryParse(qAmount, out var parsedAmount))
            {
                request.ActualAmount = parsedAmount;
            }
            if (string.IsNullOrWhiteSpace(request.Notes))
            {
                request.Notes = "Cierre de caja registrado desde la aplicación";
            }

            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound(new { code = "CASH_REGISTER_NOT_FOUND", message = "La caja especificada no fue encontrada." });
            if (current.Status == CashRegisterStatus.CLOSED)
            {
                return Ok(new CashRegisterCloseResult(current.InitialAmount, request.ActualAmount, 0m, "EXACT", true, false));
            }
            if (current.RowVersion.Length > 0 && Request.Headers.TryGetValue("If-Match", out var ifMatch))
            {
                var token = ifMatch.ToString().Trim().Trim('"');
                if (!string.Equals(token, Convert.ToBase64String(current.RowVersion), StringComparison.Ordinal))
                    return StatusCode(StatusCodes.Status412PreconditionFailed, new { code = "CASH_REGISTER_CHANGED" });
            }
            var result = await _service.CloseAsync(id, request, _currentUser.UserId);
            return Ok(result);
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
        public sealed class CloseCashRegisterRequest : CloseCashSessionRequest
        {
        }
    }
}
