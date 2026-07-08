using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Onion.BusinessLogic.Mapping;
using Onion.BusinessLogic.Profiles;
using Onion.BusinessLogic.Services.Abstract;
using Onion.BusinessLogic.Services.Concrete;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;

namespace Onion.BussinesLogic.Configurations.DependencyInjection;

public static class ServicesConfiguration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<CategoryProfile>();
        });

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<Onion.Common.Features.IFeatureService, FeatureService>();
        services.AddScoped<Onion.BussinesLogic.Services.Abstract.IAccountReceivableService, Onion.BussinesLogic.Services.Concrete.AccountReceivableService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAuthService, AuthService>();

        // Warehouse services
        services.AddScoped<IWarehouseService, WarehouseService>();

        // Sales domain service
        services.AddScoped<ISaleService, SaleService>();
        // Cash services
        services.AddScoped<ICashRegisterService, CashRegisterService>();
        services.AddScoped<ICashMovementService, CashMovementService>();
        // Register test-friendly repositories/services if needed

        // Domain services
        services.AddScoped<IUserService, UserService>();
        // Email service (optional SMTP)
        services.AddSingleton<Onion.Common.Services.IEmailService, Onion.Common.Services.SmtpEmailService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ISaleService, SaleService>();

        return services;
    }
}