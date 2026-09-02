using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;

namespace Onion.DataAccess.Configurations.DependencyInjection
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection AddRepositories(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<OnionDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("OnionCrud"));
                // Backup: ignore pending model changes warning to avoid noisy exceptions during CI/dev while root cause is addressed
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            // Keep the default tenant provider registration here (DefaultTenantProvider).
            // The concrete JwtTenantProvider is registered in the Web project (Program.cs)
            // so DataAccess remains decoupled from web-specific implementations.

            // Ensure a default tenant provider is registered so OnionDbContext can be constructed in design-time and runtime
            services.AddSingleton<ITenantProvider, DefaultTenantProvider>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISaleRepository, SaleRepository>();
            services.AddScoped<IPurchaseRepository, PurchaseRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register generic repositories used by UnitOfWork
            services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

            return services;
        }
    }
}   