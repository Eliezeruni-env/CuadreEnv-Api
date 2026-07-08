using System.Collections.Generic;
using System.Security.Claims;
using Onion.Common.Authorization;
using Xunit;

namespace Onion.Tests
{
    public class RequireRoleAttributeTests
    {
        [Fact]
        public void Allows_When_Role_Matches()
        {
            var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            var attr = new RequireRoleAttribute(false, Roles.Admin);
            var allowed = attr.CheckAccess(user);
            Assert.True(allowed);
        }

        [Fact]
        public void Forbids_When_Tenant_Mismatch()
        {
            var claims = new[] { new Claim(ClaimTypes.Role, "Admin"), new Claim("CompanyId", "2") };
            var identity = new ClaimsIdentity(claims, "test");
            var user = new ClaimsPrincipal(identity);

            var attr = new RequireRoleAttribute(requireTenantMatch: true, Roles.Admin);
            var allowed = attr.CheckAccess(user, routeValues: new Dictionary<string, object> { { "companyId", 3 } });

            Assert.False(allowed);
        }
    }
}
