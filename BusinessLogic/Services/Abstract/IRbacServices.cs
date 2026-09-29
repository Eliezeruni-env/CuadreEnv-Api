using Onion.Domain.Authorization;

namespace Onion.BussinesLogic.Services.Abstract;

public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyCollection<int> PermissionIds);
public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyCollection<int> PermissionIds);
public sealed record RoleDto(int Id, int CompanyId, string Name, string? Description, bool IsSystemRole, IReadOnlyCollection<int> PermissionIds);
public sealed record PermissionDto(int Id, string Module, string Action, string? Description);

public interface IRoleService
{
    Task<IReadOnlyCollection<RoleDto>> GetAsync();
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request);
    Task DeleteAsync(int id);
}

public interface IPermissionService
{
    Task<IReadOnlyCollection<PermissionDto>> GetAsync();
}
