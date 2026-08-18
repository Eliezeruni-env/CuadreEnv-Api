using System.Security.Claims;
using Onion.Common.Authorization;
using Xunit;

namespace Onion.Tests
{
    public class AuthorizationServiceTests
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
        public void IsInRole_Recognizes_Role_Claim()
        {
            var svc = new AuthorizationService();
            var user = CreateUser(RolesConstants.Admin, 1);

            Assert.True(svc.IsInRole(user, RolesConstants.Admin));
            Assert.False(svc.IsInRole(user, RolesConstants.Employee));
        }

        [Fact]
        public void HasPermission_Uses_Mapping()
        {
            var svc = new AuthorizationService();
            var admin = CreateUser(RolesConstants.Admin, 1);
            var mgr = CreateUser(RolesConstants.Manager, 1);
            var emp = CreateUser(RolesConstants.Employee, 1);

            Assert.True(svc.HasPermission(admin, RolesConstants.Permission_Company_Edit));
            Assert.False(svc.HasPermission(mgr, RolesConstants.Permission_Company_Edit));
            Assert.False(svc.HasPermission(emp, RolesConstants.Permission_Company_Edit));

            Assert.True(svc.HasPermission(mgr, RolesConstants.Permission_Company_Read));
            Assert.True(svc.HasPermission(admin, RolesConstants.Permission_Company_Read));
            Assert.False(svc.HasPermission(emp, RolesConstants.Permission_Settings_Edit));
        }

        [Fact]
        public void CheckTenantMatch_Compares_CompanyId()
        {
            var svc = new AuthorizationService();
            var user = CreateUser(RolesConstants.Manager, 42);

            Assert.True(svc.CheckTenantMatch(user, 42));
            Assert.False(svc.CheckTenantMatch(user, 7));
        }

        [Fact]
        public void AuthorizeAsync_Role_And_Permission_Behavior()
        {
            var svc = new AuthorizationService();
            var admin = CreateUser(RolesConstants.Admin, 1);
            var mgr = CreateUser(RolesConstants.Manager, 1);

            // Role requirement satisfied
            Assert.True(svc.AuthorizeAsync(admin, requiredRoles: new[] { RolesConstants.Admin }).Result);
            Assert.False(svc.AuthorizeAsync(mgr, requiredRoles: new[] { RolesConstants.Admin }).Result);

            // Permission requirement satisfied for manager (Company.Read)
            Assert.True(svc.AuthorizeAsync(mgr, requiredPermissions: new[] { RolesConstants.Permission_Company_Read }).Result);
            Assert.False(svc.AuthorizeAsync(mgr, requiredPermissions: new[] { RolesConstants.Permission_Settings_Edit }).Result);
        }
    }
}
