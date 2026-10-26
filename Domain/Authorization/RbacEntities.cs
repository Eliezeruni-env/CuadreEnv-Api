using System;
using Onion.Domain;
using Onion.Domain.Users;

namespace Onion.Domain.Authorization;

public interface ITenantEntity
{
    int CompanyId { get; set; }
}

public class Role : BaseEntity, ITenantEntity
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Permission : BaseEntity
{
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission : Onion.Common.Models.IAuditableEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? ModificationDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}

public class UserRole : ITenantEntity, Onion.Common.Models.IAuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int CompanyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public bool Active { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime? ModificationDate { get; set; }
    public string CreateBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}

public class DeletionApprovalRequest : BaseEntity, ITenantEntity
{
    public int CompanyId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public string Status { get; set; } = "Pending";
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}
