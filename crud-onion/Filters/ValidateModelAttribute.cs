using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Onion.Common.Models;

namespace Onion.Controllers.Filters
{
    /// <summary>
    /// Global model validation filter that returns a consistent ApiResponse&lt;object&gt; on validation failures.
    /// This prevents framework-default ProblemDetails responses and makes the frontend error shape stable.
    /// </summary>
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .ToArray();

                var resp = ApiResponse<object>.Fail("Validation failed", errors);

                context.Result = new BadRequestObjectResult(resp);
            }

            base.OnActionExecuting(context);
        }
    }
}
