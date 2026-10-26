using System;
using System.Linq;
using System.Reflection;
using Onion.Common.Models;
using Onion.Domain;
using Onion.Domain.Authorization;
using Xunit;

namespace Onion.Tests;

public sealed class AuditingTests
{
    [Fact]
    public void BaseEntity_Contains_Required_Audit_Fields()
    {
        AssertAuditContract(typeof(BaseEntity));
    }

    [Fact]
    public void Rbac_Join_Entities_Contain_Required_Audit_Fields()
    {
        AssertAuditContract(typeof(RolePermission));
        AssertAuditContract(typeof(UserRole));
    }

    private static void AssertAuditContract(Type type)
    {
        Assert.NotNull(type.GetProperty(nameof(IAuditableEntity.CreationDate)));
        Assert.NotNull(type.GetProperty(nameof(IAuditableEntity.ModificationDate)));
        Assert.NotNull(type.GetProperty(nameof(IAuditableEntity.CreateBy)));
        Assert.NotNull(type.GetProperty(nameof(IAuditableEntity.ModifiedBy)));
        Assert.NotNull(type.GetProperty("Active"));
        Assert.NotNull(type.GetProperty("IsDeleted"));
        Assert.Equal(typeof(DateTime), type.GetProperty(nameof(IAuditableEntity.CreationDate))!.PropertyType);
        Assert.Equal(typeof(DateTime?), type.GetProperty(nameof(IAuditableEntity.ModificationDate))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(IAuditableEntity.CreateBy))!.PropertyType);
        Assert.Equal(typeof(string), type.GetProperty(nameof(IAuditableEntity.ModifiedBy))!.PropertyType);
    }
}
