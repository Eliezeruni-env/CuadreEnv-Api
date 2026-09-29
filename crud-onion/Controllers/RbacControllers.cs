using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Authorization;

namespace Onion.Controllers;

[ApiController]
[Route("roles")]
[RequireRole("Admin", "SuperAdmin")]
// Tenant isolation is enforced at the service layer; keep role endpoints protected by the RequireRole filter.
public sealed class RoleController : ControllerBase
{
    private readonly IRoleService _service;
    public RoleController(IRoleService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAsync());
    [HttpPost] public async Task<IActionResult> Post(CreateRoleRequest request) => Ok(await _service.CreateAsync(request));
    [HttpPut("{id:int}")] public async Task<IActionResult> Put(int id, UpdateRoleRequest request) => Ok(await _service.UpdateAsync(id, request));
    [HttpDelete("{id:int}")] public async Task<IActionResult> Delete(int id) { await _service.DeleteAsync(id); return NoContent(); }
}

[ApiController]
[Route("permissions")]
[RequireRole("Admin", "SuperAdmin")]
public sealed class PermissionController : ControllerBase
{
    private readonly IPermissionService _service;
    public PermissionController(IPermissionService service) => _service = service;
    [HttpGet] public async Task<IActionResult> Get() => Ok(await _service.GetAsync());
}
