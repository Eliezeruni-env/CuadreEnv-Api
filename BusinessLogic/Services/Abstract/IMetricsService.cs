using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract;

public interface IMetricsService
{
    Task<MetricsSummaryDto> GetSummaryAsync(string? period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesEvolutionPoint>> GetSalesEvolutionAsync(string? period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductMetric>> GetTopProductsAsync(int limit, CancellationToken cancellationToken = default);
}
