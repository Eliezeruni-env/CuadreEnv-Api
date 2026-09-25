namespace Onion.BussinesLogic.Dtos;

public sealed record PaymentMethodMetric(decimal Amount, decimal Percentage);

public sealed record MetricsSummaryDto(
    decimal TotalRevenue,
    int TotalSalesCount,
    decimal AverageTicket,
    decimal TotalCashIn,
    decimal TotalCashOut,
    int TotalCustomersCount,
    decimal TotalUnitsSold,
    decimal RevenueGrowthPercentage,
    IReadOnlyDictionary<string, PaymentMethodMetric> PaymentMethodsBreakdown);

public sealed record SalesEvolutionPoint(string Label, decimal Amount, int SalesCount);

public sealed record TopProductMetric(int ProductId, string Name, decimal Units, decimal Revenue, decimal Percentage);
