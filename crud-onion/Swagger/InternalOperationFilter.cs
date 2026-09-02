using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Onion.Controllers.Swagger
{
    // Adds security requirement for internal endpoints requiring the X-Internal-ApiKey header
    public class InternalOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var relPath = context.ApiDescription.RelativePath ?? string.Empty;
            // relative paths in ApiDescription don't start with a leading '/'
            if (relPath.StartsWith("internal") || relPath.StartsWith("internal/"))
            {
                // Ensure operation.Security exists
                if (operation.Security == null)
                    operation.Security = new List<OpenApiSecurityRequirement>();

                var scheme = new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "InternalApiKey" }
                };

                var requirement = new OpenApiSecurityRequirement
                {
                    [ scheme ] = new List<string>()
                };

                operation.Security.Add(requirement);

                // Tag internal endpoints explicitly
                operation.Tags = operation.Tags ?? new List<OpenApiTag>();
                if (!operation.Tags.Any(t => t.Name == "internal"))
                    operation.Tags.Add(new OpenApiTag { Name = "internal" });
            }
        }
    }
}
