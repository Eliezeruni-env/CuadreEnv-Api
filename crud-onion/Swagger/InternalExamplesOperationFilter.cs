using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Onion.Controllers.Swagger
{
    public class InternalExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var relPath = context.ApiDescription.RelativePath ?? string.Empty;
            if (!(relPath.StartsWith("internal") || relPath.StartsWith("internal/"))) return;

            if (operation.RequestBody == null) return;

            foreach (var content in operation.RequestBody.Content)
            {
                var media = content.Value;
                // Provide examples for known internal endpoints
                if (relPath.StartsWith("internal/users") && relPath.Contains("subscription/by-plan-code") && media.Examples.Count == 0)
                {
                    media.Examples.Add("example", new OpenApiExample { Value = new OpenApiObject {
                        ["planCode"] = new OpenApiString("pro-monthly"),
                        ["startsAt"] = new OpenApiString("2026-09-01T00:00:00Z"),
                        ["nextPaymentAt"] = new OpenApiString("2026-09-30T00:00:00Z")
                    }});
                }

                if (relPath.StartsWith("internal/users") && relPath.EndsWith("/status") && media.Examples.Count == 0)
                {
                    media.Examples.Add("example", new OpenApiExample { Value = new OpenApiObject {
                        ["newStatus"] = new OpenApiString("Blocked"),
                        ["changedBy"] = new OpenApiString("um-admin"),
                        ["reason"] = new OpenApiString("violation")
                    }});
                }

                if (relPath.StartsWith("internal/subscriptions") && relPath.Contains("next-payment") && media.Examples.Count == 0)
                {
                    media.Examples.Add("example", new OpenApiExample { Value = new OpenApiObject {
                        ["nextPaymentAt"] = new OpenApiString("2026-10-15T00:00:00Z")
                    }});
                }
            }
        }
    }
}
