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
            cfg.AddProfile<CompanyProfile>();
        });

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<Onion.Common.Features.IFeatureService, FeatureService>();
        services.AddScoped<Onion.BussinesLogic.Services.Abstract.IAccountReceivableService, Onion.BussinesLogic.Services.Concrete.AccountReceivableService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductTypeService, ProductTypeService>();
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
        // Pagination service for server-side helpers
        services.AddScoped<Onion.BussinesLogic.Services.Abstract.IPaginationService, Onion.BussinesLogic.Services.Infrastructure.PaginationService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<Onion.BussinesLogic.Services.Abstract.ICreditService, Onion.BussinesLogic.Services.Concrete.CreditService>();
        services.AddScoped(typeof(Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Credits.Credit>), typeof(Onion.DataAccess.Repositories.Concrete.GenericRepository<Onion.Domain.Credits.Credit>));
        services.AddScoped(typeof(Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Credits.CreditPayment>), typeof(Onion.DataAccess.Repositories.Concrete.GenericRepository<Onion.Domain.Credits.CreditPayment>));
        services.AddScoped(typeof(Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Credits.CreditStatusHistory>), typeof(Onion.DataAccess.Repositories.Concrete.GenericRepository<Onion.Domain.Credits.CreditStatusHistory>));
        // Appointments module
        services.AddScoped<Onion.BussinesLogic.Services.Abstract.IAppointmentService, Onion.BussinesLogic.Services.Concrete.AppointmentService>();

        // Current user / tenant service (reads claims from HttpContext)
        services.AddScoped<Onion.Common.Services.ICurrentUserService, Onion.Common.Services.CurrentUserService>();

        return services;
    }
}