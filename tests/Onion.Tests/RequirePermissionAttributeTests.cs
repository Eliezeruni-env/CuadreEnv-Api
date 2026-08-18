using System.Collections.Generic;
using System.Security.Claims;
using Onion.Common.Authorization;
using Xunit;

namespace Onion.Tests
{
    public class RequirePermissionAttributeTests
    {
        private ClaimsPrincipal CreateUser(string? role = null, int? companyId = null)
        {
            var claims = new List<Claim>();
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
            if (companyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public void CheckAccess_Allows_When_Role_Has_Permission()
        {
            var auth = new AuthorizationService();
            // Test permission-based access (Company.Read) for Manager
            var filter = new RequirePermissionFilter(auth, new string[0], new[] { RolesConstants.Permission_Company_Read }, null);
            var user = CreateUser(RolesConstants.Manager, 5);

            var ok = filter.CheckAccess(user, new Dictionary<string, object?> { { "companyId", 5 } }, null);
            Assert.True(ok);
        }

        [Fact]
        public void CheckAccess_Forbids_When_No_Role_Or_Permission()
        {
            var auth = new AuthorizationService();
            var filter = new RequirePermissionFilter(auth, new[] { RolesConstants.Admin }, null, null);
            var user = CreateUser(RolesConstants.Employee, 1);

            var ok = filter.CheckAccess(user, new Dictionary<string, object?> { { "companyId", 1 } }, null);
            Assert.False(ok);
        }

        [Fact]
        public void CheckAccess_Allows_Tenant_Match_When_No_Roles_Or_Permissions()
        {
            var auth = new AuthorizationService();
            var filter = new RequirePermissionFilter(auth, new string[0], new string[0], null);
            var user = CreateUser(RolesConstants.Employee, 99);

            var ok = filter.CheckAccess(user, new Dictionary<string, object?> { { "companyId", 99 } }, null);
            Assert.True(ok);
        }
    }
}
