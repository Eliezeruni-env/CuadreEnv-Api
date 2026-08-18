using System.Collections.Generic;
using System.Security.Claims;
using Onion.Common.Authorization;
using Xunit;

namespace Onion.Tests
{
    public class RequirePermissionCompanyControllerTests
    {
        private ClaimsPrincipal CreateUser(string? role = null, int? companyId = null)
        {
            var claims = new System.Collections.Generic.List<Claim>();
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
        public void Company_Get_Allows_SuperAdmin_Or_Owner()
        {
            var auth = new AuthorizationService();

            // Attribute configured to allow SuperAdmin role OR tenant owner (route param 'id')
            var filter = new RequirePermissionFilter(auth, new[] { RolesConstants.SuperAdmin }, new string[0], "id");

            var superAdmin = CreateUser(RolesConstants.SuperAdmin, null);
            var owner = CreateUser(RolesConstants.Employee, 42);
            var other = CreateUser(RolesConstants.Employee, 7);

            Assert.True(filter.CheckAccess(superAdmin, new Dictionary<string, object?> { { "id", 100 } }, null));
            Assert.True(filter.CheckAccess(owner, new Dictionary<string, object?> { { "id", 42 } }, null));
            Assert.False(filter.CheckAccess(other, new Dictionary<string, object?> { { "id", 42 } }, null));
        }
    }
}
