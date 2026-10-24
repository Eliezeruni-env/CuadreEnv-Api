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
                if (ctx.Items.TryGetValue("TenantCompanyId", out var resolved) &&
                    resolved is int resolvedId && resolvedId > 0)
                    return resolvedId;

                var header = ctx.Request.Headers["X-Company-Id"].FirstOrDefault();
                if (int.TryParse(header, out var headerId) && headerId > 0)
                    return headerId;

                var claim = ctx.User?.FindFirst("companyId") ?? ctx.User?.FindFirst("CompanyId");
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
