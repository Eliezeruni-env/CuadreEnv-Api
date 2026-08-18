using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Onion.Common.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? CompanyId
        {
            get
            {
                try
                {
                    var ctx = _httpContextAccessor.HttpContext;
                    var claim = ctx?.User?.FindFirst("CompanyId");
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

        public int? UserId
        {
            get
            {
                try
                {
                    var ctx = _httpContextAccessor.HttpContext;
                    var claim = ctx?.User?.FindFirst(ClaimTypes.NameIdentifier);
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

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }
}
