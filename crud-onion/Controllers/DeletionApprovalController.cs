using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;

namespace Onion.Controllers;

[ApiController]
[Route("approvals")]
public sealed class DeletionApprovalController : ControllerBase
{
    private readonly IDeletionApprovalService _service;
    public DeletionApprovalController(IDeletionApprovalService service) => _service = service;

    [HttpGet]
    [RequireRole("Admin", "SuperAdmin")]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetPendingAsync());

    [HttpGet("pending")]
    [RequireRole("Admin", "SuperAdmin")]
    public async Task<IActionResult> Pending() => Ok(await _service.GetPendingAsync());

    [HttpPost("{id:int}/approve")]
    [RequireRole("Admin", "SuperAdmin")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovalReviewRequest request) { await _service.ApproveAsync(id, request.Notes); return Ok(); }

    [HttpPost("{id:int}/reject")]
    [RequireRole("Admin", "SuperAdmin")]
    public async Task<IActionResult> Reject(int id, [FromBody] ApprovalReviewRequest request) { await _service.RejectAsync(id, request.Notes); return Ok(); }

    public sealed record ApprovalReviewRequest(string? Notes);
}
