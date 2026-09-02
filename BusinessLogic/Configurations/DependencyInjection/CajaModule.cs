using Microsoft.Extensions.DependencyInjection;
using Onion.BussinesLogic.Background;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;

namespace Onion.BussinesLogic.Configurations.DependencyInjection
{
    public static class CajaModule
    {
        public static IServiceCollection AddCajaModule(this IServiceCollection services)
        {
            services.AddSingleton<InMemoryBackgroundQueue>();
            services.AddSingleton<IBackgroundQueue>(sp => sp.GetRequiredService<InMemoryBackgroundQueue>());
            services.AddHostedService<BackgroundWorker>();
            services.AddScoped<ICajaService, CajaService>();
            return services;
        }
    }
}
