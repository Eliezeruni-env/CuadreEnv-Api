using Onion.Domain.Finance;
using Xunit;

namespace Onion.Domain.Tests;

public class CashDiscrepancyTests
{
    [Fact]
    public void Calculate_ExactCount_ReturnsZeroDifference()
    {
        var result = CashDiscrepancy.Calculate(100.005m, 100.004m);

        Assert.Equal(100.00m, result.ExpectedAmount);
        Assert.Equal(100.00m, result.ActualAmount);
        Assert.Equal(0.00m, result.DifferenceAmount);
        Assert.True(result.IsExact);
        Assert.False(result.IsSurplus);
        Assert.False(result.IsShortage);
    }

    [Fact]
    public void Calculate_Surplus_ReturnsPositiveDifference()
    {
        var result = CashDiscrepancy.Calculate(100m, 101.25m);

        Assert.Equal(1.25m, result.DifferenceAmount);
        Assert.True(result.IsSurplus);
    }

    [Fact]
    public void Calculate_Shortage_ReturnsNegativeDifference()
    {
        var result = CashDiscrepancy.Calculate(100m, 98.75m);

        Assert.Equal(-1.25m, result.DifferenceAmount);
        Assert.True(result.IsShortage);
    }
}
