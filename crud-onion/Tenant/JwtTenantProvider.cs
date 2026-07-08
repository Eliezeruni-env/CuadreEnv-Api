using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Onion.DataAccess;

namespace Onion.DataAccess.Tenant
{
    public class JwtTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCompanyId()
        {
            try
            {
                var ctx = _httpContextAccessor.HttpContext;
                if (ctx == null) return null;
                var claim = ctx.User?.FindFirst("CompanyId");
                if (claim == null) return null;
                if (int.TryParse(claim.Value, out var id)) return id;
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
