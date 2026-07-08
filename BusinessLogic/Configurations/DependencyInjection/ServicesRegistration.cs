using Microsoft.Extensions.DependencyInjection;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;

namespace Onion.BussinesLogic.Configurations.DependencyInjection
{
    public static class ServicesRegistration
    {
        public static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ISupplierService, SupplierService>();
            services.AddScoped<IAuthService, AuthService>();
            // Background cleanup not registered here to avoid extra hosting dependencies.
            // other services already registered elsewhere
            return services;
        }
    }
}
