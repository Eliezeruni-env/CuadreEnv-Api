using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Onion.Common.Models;
using Onion.Domain.Finance;

namespace Onion.Controllers;

[ApiController]
[Route("~/api/v1/taxpayers")]
[Route("~/api/v1/dgii/taxpayers")]
[AuthorizeModule("BILLING")]
public sealed class TaxpayerController : ControllerBase
{
    private readonly ITaxpayerService _service;
    public TaxpayerController(ITaxpayerService service) => _service = service;

    [HttpGet("{rncOrCedula}")]
    public async Task<ActionResult<ApiResponse<TaxpayerDto>>> Get(string rncOrCedula, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(rncOrCedula, cancellationToken);
        return result is null ? NotFound(ApiResponse<TaxpayerDto>.Fail("Taxpayer not found.", new[] { "TAXPAYER_NOT_FOUND" })) : Ok(ApiResponse<TaxpayerDto>.Ok(result));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<TaxpayerDto>>> Upsert([FromBody] Taxpayer taxpayer, CancellationToken cancellationToken)
        => Ok(ApiResponse<TaxpayerDto>.Ok(await _service.UpsertAsync(taxpayer, cancellationToken)));
}
