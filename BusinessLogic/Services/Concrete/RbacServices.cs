using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain.Authorization;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class RoleService : IRoleService
{
    private readonly OnionDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RoleService(OnionDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private int CompanyId => _currentUser.CompanyId ?? throw new InvalidOperationException("Tenant company id is required.");

    public async Task<IReadOnlyCollection<RoleDto>> GetAsync() => await _db.Roles
        .AsNoTracking()
        .Include(r => r.RolePermissions)
        .Where(r => r.CompanyId == CompanyId)
        .Select(r => new RoleDto(r.Id, r.CompanyId, r.Name, r.Description, r.IsSystemRole, r.RolePermissions.Select(x => x.PermissionId).ToArray()))
        .ToListAsync();

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
    {
        Validate(request.Name);
        var permissionIds = await ValidPermissionIds(request.PermissionIds);
        var role = new Role { CompanyId = CompanyId, Name = request.Name.Trim(), Description = request.Description, CreatedBy = _currentUser.UserId, IsSystemRole = false };
        role.RolePermissions = permissionIds.Select(id => new RolePermission { PermissionId = id, Role = role }).ToList();
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return new RoleDto(role.Id, role.CompanyId, role.Name, role.Description, role.IsSystemRole, permissionIds.ToArray());
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request)
    {
        Validate(request.Name);
        var role = await _db.Roles.Include(r => r.RolePermissions).SingleOrDefaultAsync(r => r.Id == id && r.CompanyId == CompanyId)
            ?? throw new KeyNotFoundException("Role not found.");
        if (role.IsSystemRole) throw new InvalidOperationException("System roles cannot be edited.");
        var permissionIds = await ValidPermissionIds(request.PermissionIds);
        role.Name = request.Name.Trim();
        role.Description = request.Description;
        _db.RolePermissions.RemoveRange(role.RolePermissions);
        role.RolePermissions = permissionIds.Select(permissionId => new RolePermission { RoleId = role.Id, PermissionId = permissionId }).ToList();
        await _db.SaveChangesAsync();
        return new RoleDto(role.Id, role.CompanyId, role.Name, role.Description, role.IsSystemRole, permissionIds.ToArray());
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Id == id && r.CompanyId == CompanyId)
            ?? throw new KeyNotFoundException("Role not found.");
        if (role.IsSystemRole) throw new InvalidOperationException("System roles cannot be deleted.");
        if (await _db.UserRoles.AnyAsync(x => x.RoleId == id)) throw new InvalidOperationException("Role has assigned users.");
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
    }

    private async Task<List<int>> ValidPermissionIds(IEnumerable<int> ids)
    {
        var requested = ids.Distinct().ToList();
        var found = await _db.Permissions.Where(p => requested.Contains(p.Id)).Select(p => p.Id).ToListAsync();
        if (found.Count != requested.Count) throw new InvalidOperationException("One or more permissions are invalid.");
        return found;
    }

    private static void Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name is required.");
    }
}

public sealed class PermissionService : IPermissionService
{
    private readonly OnionDbContext _db;
    public PermissionService(OnionDbContext db) => _db = db;
    public async Task<IReadOnlyCollection<PermissionDto>> GetAsync() => await _db.Permissions.AsNoTracking()
        .OrderBy(p => p.Module).ThenBy(p => p.Action)
        .Select(p => new PermissionDto(p.Id, p.Module, p.Action, p.Description)).ToListAsync();
}
