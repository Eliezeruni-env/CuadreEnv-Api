using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Onion.Common.Mvc
{
    // Application model convention to prepend a route prefix (e.g., "v1") to all controllers.
    public class RoutePrefixConvention : IApplicationModelConvention
    {
        private readonly AttributeRouteModel _routePrefix;

        public RoutePrefixConvention(string prefix)
        {
            // normalize prefix to avoid leading/trailing slashes
            var p = prefix?.Trim('/') ?? string.Empty;
            _routePrefix = new AttributeRouteModel(new RouteAttribute(p));
        }

        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var selector in controller.Selectors)
                {
                    if (selector.AttributeRouteModel == null)
                    {
                        // controller has no route attribute -> apply prefix directly
                        selector.AttributeRouteModel = _routePrefix;
                    }
                    else
                    {
                        // combine prefix with existing attribute route
                        selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
                    }
                }
            }
        }
    }
}
