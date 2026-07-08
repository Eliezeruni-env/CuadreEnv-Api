using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Onion.Common.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _featureKey;

        public RequireFeatureAttribute(string featureKey)
        {
            _featureKey = featureKey;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var companyClaim = user.FindFirst("CompanyId")?.Value;
            if (!int.TryParse(companyClaim, out var companyId))
            {
                context.Result = new ForbidResult();
                return;
            }

            var svc = context.HttpContext.RequestServices.GetService(typeof(Onion.Common.Features.IFeatureService)) as Onion.Common.Features.IFeatureService;
            if (svc == null)
            {
                // If no service, deny by default
                context.Result = new ForbidResult();
                return;
            }

            var ok = await svc.CompanyHasFeatureAsync(companyId, _featureKey);
            if (!ok)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
