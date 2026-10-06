using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Onion.Common.Models;
using Onion.Common.Services;
using Onion.Domain.Invoices;

namespace Onion.Controllers;

[ApiController]
[Route("~/api/v1/ncf-sequences")]
[Route("~/api/v1/fiscal-sequences")]
[AuthorizeModule("BILLING")]
public sealed class FiscalSequenceController : ControllerBase
{
    private readonly IFiscalSequenceService _service;
    private readonly ICurrentUserService _currentUser;

    public FiscalSequenceController(IFiscalSequenceService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FiscalSequenceSummaryDto>>>> Get(CancellationToken cancellationToken)
    {
        var companyId = RequireCompany();
        return Ok(ApiResponse<IReadOnlyList<FiscalSequenceSummaryDto>>.Ok(await _service.GetActiveAsync(companyId, cancellationToken)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FiscalSequenceSummaryDto>>> Create([FromBody] CreateFiscalSequenceRequest request, CancellationToken cancellationToken)
    {
        var companyId = RequireCompany();
        var result = await _service.CreateAsync(companyId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), ApiResponse<FiscalSequenceSummaryDto>.Ok(result, "Fiscal sequence created."));
    }

    [HttpPost("request-next")]
    [HttpPost("next")]
    public async Task<ActionResult<ApiResponse<string>>> Next([FromBody] NextFiscalNumberRequest request, CancellationToken cancellationToken)
    {
        var companyId = RequireCompany();
        var number = await _service.NextFiscalNumberAsync(companyId, request.VoucherType, cancellationToken);
        return Ok(ApiResponse<string>.Ok(number));
    }

    private int RequireCompany()
    {
        var id = _currentUser.CompanyId;
        return id is > 0 ? id.Value : throw new InvalidOperationException("A valid company is required.");
    }
}

public sealed record NextFiscalNumberRequest(VoucherType VoucherType);
