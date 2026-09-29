namespace Onion.Domain.Finance;

public readonly record struct CashDiscrepancy(decimal ExpectedAmount, decimal ActualAmount, decimal DifferenceAmount)
{
    public bool IsExact => DifferenceAmount == 0m;
    public bool IsSurplus => DifferenceAmount > 0m;
    public bool IsShortage => DifferenceAmount < 0m;

    public static CashDiscrepancy Calculate(decimal expectedAmount, decimal actualAmount, int currencyScale = 2)
    {
        if (currencyScale is < 0 or > 4)
            throw new ArgumentOutOfRangeException(nameof(currencyScale));
        if (expectedAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(expectedAmount));
        if (actualAmount < 0m)
            throw new ArgumentOutOfRangeException(nameof(actualAmount));

        var expected = decimal.Round(expectedAmount, currencyScale, MidpointRounding.ToEven);
        var actual = decimal.Round(actualAmount, currencyScale, MidpointRounding.ToEven);
        return new CashDiscrepancy(expected, actual,
            decimal.Round(actual - expected, currencyScale, MidpointRounding.ToEven));
    }
}
