using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;
using Onion.Common.Services;
using Onion.Common.Models;

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
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get607(int year, int month, CancellationToken cancellationToken) => GetResultAsync(() => _service.Get607Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("607.txt")]
    public async Task<IActionResult> Download607(int year, int month, CancellationToken cancellationToken) => DownloadAsync(await _service.Get607Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("607/download")]
    public async Task<IActionResult> Download607Canonical(int year, int month, CancellationToken cancellationToken) => await Download607(year, month, cancellationToken);

    [HttpGet("606")]
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get606(int year, int month, CancellationToken cancellationToken) => GetResultAsync(() => _service.Get606Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("606.txt")]
    public async Task<IActionResult> Download606(int year, int month, CancellationToken cancellationToken) => DownloadAsync(await _service.Get606Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("606/download")]
    public async Task<IActionResult> Download606Canonical(int year, int month, CancellationToken cancellationToken) => await Download606(year, month, cancellationToken);

    [HttpGet("608")]
    public Task<ActionResult<ApiResponse<DgiiReportResult>>> Get608(int year, int month, CancellationToken cancellationToken) => GetResultAsync(() => _service.Get608Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("608.txt")]
    public async Task<IActionResult> Download608(int year, int month, CancellationToken cancellationToken) => DownloadAsync(await _service.Get608Async(CompanyId(), year, month, cancellationToken));

    [HttpGet("608/download")]
    public async Task<IActionResult> Download608Canonical(int year, int month, CancellationToken cancellationToken) => await Download608(year, month, cancellationToken);

    private async Task<ActionResult<ApiResponse<DgiiReportResult>>> GetResultAsync(Func<Task<DgiiReportResult>> factory) => Ok(ApiResponse<DgiiReportResult>.Ok(await factory()));
    private FileContentResult DownloadAsync(DgiiReportResult result) => File(System.Text.Encoding.UTF8.GetBytes(result.ToPipeDelimited() + Environment.NewLine), "text/plain; charset=utf-8", $"DGII-{result.Format}-{result.Year:D4}{result.Month:D2}.txt");
    private int CompanyId()
    {
        var id = _currentUser.CompanyId;
        return id is > 0 ? id.Value : throw new InvalidOperationException("A valid company is required.");
    }
}
