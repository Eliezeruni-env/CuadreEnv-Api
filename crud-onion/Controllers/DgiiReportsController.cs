using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Onion.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Onion.Controllers;

[ApiController]
[Route("dgii")]
[AuthorizeModule("REPORTS")]
public sealed class DgiiReportsController : ControllerBase
{
    private readonly IDgiiReportService _service;
    private readonly ICurrentUserService _currentUser;

    public DgiiReportsController(IDgiiReportService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet("607")]
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get607([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => GetReportAsync(year, month, () => _service.Get607Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("607.txt")]
    public async Task<IActionResult> Download607([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => IsValidPeriod(year, month) ? DownloadAsync(await _service.Get607Async(CompanyId(), year, month, cancellationToken)) : InvalidPeriod();

    [HttpGet("607/download")]
    public async Task<IActionResult> Download607Canonical([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => await Download607(year, month, cancellationToken);

    [HttpGet("606")]
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get606([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => GetReportAsync(year, month, () => _service.Get606Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("606.txt")]
    public async Task<IActionResult> Download606([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => IsValidPeriod(year, month) ? DownloadAsync(await _service.Get606Async(CompanyId(), year, month, cancellationToken)) : InvalidPeriod();

    [HttpGet("606/download")]
    public async Task<IActionResult> Download606Canonical([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => await Download606(year, month, cancellationToken);

    [HttpGet("608")]
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get608([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => GetReportAsync(year, month, () => _service.Get608Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("608.txt")]
    public async Task<IActionResult> Download608([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => IsValidPeriod(year, month) ? DownloadAsync(await _service.Get608Async(CompanyId(), year, month, cancellationToken)) : InvalidPeriod();

    [HttpGet("608/download")]
    public async Task<IActionResult> Download608Canonical([FromQuery, Range(2000, 2100)] int year, [FromQuery, Range(1, 12)] int month, CancellationToken cancellationToken) => await Download608(year, month, cancellationToken);

    private async Task<ActionResult<ApiResponse<DgiiReportResult>>> GetReportAsync(int year, int month, Func<Task<DgiiReportResult>> factory)
    {
        if (!IsValidPeriod(year, month)) return InvalidPeriod();
        return Ok(ApiResponse<DgiiReportResult>.Ok(await factory()));
    }

    private BadRequestObjectResult InvalidPeriod() => BadRequest(new { error = "year must be between 2000 and 2100 and month must be between 1 and 12" });
    private static bool IsValidPeriod(int year, int month) => year is >= 2000 and <= 2100 && month is >= 1 and <= 12;
    private FileContentResult DownloadAsync(DgiiReportResult result) => File(System.Text.Encoding.UTF8.GetBytes(result.ToPipeDelimited() + Environment.NewLine), "text/plain; charset=utf-8", $"DGII-{result.Format}-{result.Year:D4}{result.Month:D2}.txt");
    private int CompanyId()
    {
        var id = _currentUser.CompanyId;
        return id is > 0 ? id.Value : throw new InvalidOperationException("A valid company is required.");
    }
}
