using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;

namespace Onion.Controllers;

[Route("Metrics")]
[ApiController]
[AuthorizeModule("REPORTS")]
public sealed class MetricsController : ControllerBase
{
    private readonly IMetricsService _service;

    public MetricsController(IMetricsService service) => _service = service;

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? period = "month", CancellationToken cancellationToken = default) =>
        Ok(await _service.GetSummaryAsync(period, cancellationToken));

    [HttpGet("sales-evolution")]
    public async Task<IActionResult> SalesEvolution([FromQuery] string? period = "7D", CancellationToken cancellationToken = default) =>
        Ok(await _service.GetSalesEvolutionAsync(period, cancellationToken));

    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] int limit = 5, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetTopProductsAsync(limit, cancellationToken));
}