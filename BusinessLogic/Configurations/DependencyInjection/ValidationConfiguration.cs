using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Validators;
using Onion.Domain;
using Onion.Domain.Products;

namespace Onion.BussinesLogic.Configurations.DependencyInjection
{
    public static class ValidationConfiguration
    {
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            // Register validators explicitly to avoid needing FluentValidation.DependencyInjectionExtensions
            services.AddTransient<IValidator<RegisterRequestDto>, RegisterRequestDtoValidator>();
            services.AddTransient<IValidator<LoginRequestDto>, LoginRequestDtoValidator>();
            services.AddTransient<IValidator<Product>, ProductValidator>();
            services.AddTransient<IValidator<Company>, CompanyValidator>();
            services.AddTransient<IValidator<Onion.BussinesLogic.Dtos.SaleRequestDto>, Onion.BussinesLogic.Validators.SaleRequestDtoValidator>();
            services.AddTransient<IValidator<Onion.BussinesLogic.Dtos.SaleDetailDto>, Onion.BussinesLogic.Validators.SaleDetailDtoValidator>();
            return services;
        }
    }
}
