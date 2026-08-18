using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Onion.BussinesLogic.HostedServices
{
    public class CreditOverdueHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CreditOverdueHostedService> _logger;
        // Temporary test interval; changed to 30 seconds for validation and will be reverted later.
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

        public CreditOverdueHostedService(IServiceScopeFactory scopeFactory, ILogger<CreditOverdueHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CreditOverdueHostedService starting");

            var timer = new PeriodicTimer(_interval);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await ProcessOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            finally
            {
                timer.Dispose();
                _logger.LogInformation("CreditOverdueHostedService stopped");
            }
        }

        private async Task ProcessOnceAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Onion.DataAccess.OnionDbContext>();
            var ambient = scope.ServiceProvider.GetService<Onion.DataAccess.Tenant.AmbientTenantProvider>();
            var svc = scope.ServiceProvider.GetRequiredService<Onion.BussinesLogic.Services.Abstract.ICreditService>();

            try
            {
                // Get all company ids (ignore query filters)
                var companyIds = await db.Companies.IgnoreQueryFilters().AsNoTracking().Select(c => c.Id).ToListAsync(stoppingToken);
                foreach (var companyId in companyIds)
                {
                    try
                    {

                        if (ambient != null)
                        {
                            ambient.SetCompanyId(companyId);
                            // Also set static ambient value to ensure any async context/readers pick it up
                            Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = companyId;
                        }

                        // Call service to check overdue for that tenant
                        await svc.CheckOverdueAsync();

                        _logger.LogInformation("Processed overdue credits for company {CompanyId}", companyId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing company {CompanyId}", companyId);
                    }
                    finally
                    {
                        if (ambient != null)
                            ambient.Clear();
                        // clear static override as well
                        Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating companies in CreditOverdueHostedService");
            }
        }
    }
}
