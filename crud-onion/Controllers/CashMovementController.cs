using Microsoft.AspNetCore.Mvc;
using System;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Domain;
using Onion.Common.Services;
using Onion.Common.Exceptions;
using Onion.Common.Enums;
using Onion.Common.Authorization;
using Onion.Domain.Finance;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [Route("cash-movement")]
    [Route("cash-movements")]
    [ApiController]
    [AuthorizeModule("POS")]
    public class CashMovementController : ControllerBase
    {
        private readonly ICashMovementService _service;
        private readonly IGlobalizationService _globalizationService;
        private readonly ICurrentUserService _currentUser;

        public CashMovementController(ICashMovementService service, IGlobalizationService globalizationService, ICurrentUserService currentUser)
        {
            _service = service;
            _globalizationService = globalizationService;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? pageNumber = null, [FromQuery] int? pageSize = null, [FromQuery] int? cashRegisterId = null, [FromQuery] int? cashSessionId = null)
        {
            try
            {
                var items = (await _service.GetAllAsync()).ToList();
                if (cashRegisterId.HasValue && cashRegisterId.Value > 0)
                {
                    items = items.Where(m => m.CashRegisterId == cashRegisterId.Value).ToList();
                }
                if (cashSessionId.HasValue && cashSessionId.Value > 0)
                {
                    items = items.Where(m => m.CashSessionId == cashSessionId.Value).ToList();
                }

                if (pageNumber.HasValue)
                {
                    var pn = Math.Max(1, pageNumber.Value);
                    var ps = pageSize ?? 10;
                    var paged = items.Skip((pn - 1) * ps).Take(ps).ToList();
                    return Ok(paged);
                }

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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CashMovementRequest request)
        {
            if (_currentUser.CompanyId is not > 0) return Forbid();
            if (request.CompanyId.HasValue && request.CompanyId.Value != _currentUser.CompanyId.Value)
                return Forbid();
            if (request.Amount <= 0) return BadRequest(new { errorCode = "INVALID_MOVEMENT_AMOUNT", message = "amount debe ser mayor que cero." });
            if (!TryParseMovementType(request.Type, out var type))
                return BadRequest(new { errorCode = "INVALID_MOVEMENT_TYPE", message = "type debe ser Entrada o Retiro/Salida." });

            var cm = new CashMovement
            {
                CashRegisterId = request.CashRegisterId,
                CashSessionId = request.SessionId ?? request.CashSessionId,
                Amount = request.Amount,
                Type = type,
                Reason = request.Concept ?? request.Reason ?? request.Category ?? "Movimiento de caja",
                Description = request.Description ?? request.Concept ?? request.Category,
                CompanyId = _currentUser.CompanyId.Value
            };
            var created = await _service.AddAsync(cm);
            return CreatedAtAction(nameof(GetAll), new { id = created.Id }, created);
        }

        private static bool TryParseMovementType(object? value, out CashMovementType type)
        {
            var text = value switch
            {
                System.Text.Json.JsonElement element when element.ValueKind == System.Text.Json.JsonValueKind.Number => element.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
                System.Text.Json.JsonElement element when element.ValueKind == System.Text.Json.JsonValueKind.String => element.GetString(),
                _ => value?.ToString()
            };

            if (string.Equals(text, "Entrada", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(text, "CashIn", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "In", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
            {
                type = CashMovementType.CashIn;
                return true;
            }
            if (string.Equals(text, "Salida", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Retiro", StringComparison.OrdinalIgnoreCase) || 
                string.Equals(text, "CashOut", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Out", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "2", StringComparison.OrdinalIgnoreCase))
            {
                type = CashMovementType.CashOut;
                return true;
            }
            type = default;
            return false;
        }

        public sealed class CashMovementRequest
        {
            public object? Type { get; set; }
            public decimal Amount { get; set; }
            public string? Concept { get; set; }
            public string? Reason { get; set; }
            public string? Description { get; set; }
            public string? Category { get; set; }
            public int CashRegisterId { get; set; }
            public int? SessionId { get; set; }
            public int? CashSessionId { get; set; }
            public int? CompanyId { get; set; }
        }
    }
}
