using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Onion.Common.Models;
using Onion.Common.Services;
using Onion.Domain.Finance;

namespace Onion.Controllers;

[ApiController]
[Route("cash-sessions")]
[Route("api/caja")]
[AuthorizeModule("POS")]
public sealed class CashSessionController : ControllerBase
{
    private readonly ICashSessionService _service;
    private readonly ICurrentUserService _currentUser;

    public CashSessionController(ICashSessionService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("active-session")]
    [HttpGet("/api/caja/current")]
    public async Task<ActionResult<ApiResponse<CashSessionDto?>>> ActiveSession(CancellationToken cancellationToken)
    {
        var result = await _service.GetActiveAsync(RequireCompany(), RequireUser(), cancellationToken);
        return Ok(ApiResponse<CashSessionDto?>.Ok(result, result is null ? "No active cash session exists." : "Active cash session retrieved."));
    }

    [HttpPost("open")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> Open([FromBody] OpenCashSessionRequest request, CancellationToken cancellationToken)
    {
        var companyId = RequireCompany();
        var userId = RequireUser();
        var result = await _service.OpenAsync(companyId, request, userId, cancellationToken);
        return Ok(ApiResponse<CashSessionDto>.Ok(result, "Cash session opened."));
    }

    [HttpPost("{id:int}/movements")]
    [HttpPost("/api/caja/{id:int}/movements")]
    public async Task<ActionResult<ApiResponse<CashMovementDto>>> AddMovement(int id, [FromBody] AddCashMovementRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.AddMovementAsync(RequireCompany(), id, request, RequireUser(), cancellationToken);
        return Ok(ApiResponse<CashMovementDto>.Ok(result, "Cash movement recorded."));
    }

    [HttpPost("{id:int}/close")]
    [HttpPost("/api/caja/{id:int}/close")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> Close(int id, [FromBody] CloseCashSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CloseAsync(RequireCompany(), id, request, RequireUser(), cancellationToken);
        return Ok(ApiResponse<CashSessionDto>.Ok(result, "Cierre de caja registrado exitosamente."));
    }

    [HttpPost("sessions/{id:int}/close")]
    [HttpPost("/api/caja/sessions/{id:int}/close")]
    public async Task<ActionResult<ApiResponse<CashSessionDto>>> CloseCompatibility(int id, [FromBody] CloseCashSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CloseByRegisterOrSessionAsync(RequireCompany(), id, request, RequireUser(), cancellationToken);
        return Ok(ApiResponse<CashSessionDto>.Ok(result, "Cierre de caja registrado exitosamente."));
    }

    private int RequireCompany()
    {
        var id = _currentUser.CompanyId;
        return id is > 0 ? id.Value : throw new Onion.Common.Exceptions.TenantRequiredException("A valid company is required.");
    }

    private int RequireUser()
    {
        var id = _currentUser.UserId;
        return id is > 0 ? id.Value : throw new Onion.Common.Exceptions.TenantRequiredException("A valid user is required.");
    }
}
