using Onion.Domain;
using Onion.Domain.Invoices;
using Xunit;

namespace Onion.Domain.Tests;

public class FinancialIntegrityTests
{
    [Fact]
    public void CashRegister_Close_PersistsPhysicalCountAndPreventsSecondClose()
    {
        var register = new CashRegister
        {
            OpeningAmount = 100m,
            InitialAmount = 100m,
            Status = CashRegisterStatus.OPEN,
            IsOpen = true
        };
        var discrepancy = Onion.Domain.Finance.CashDiscrepancy.Calculate(125m, 123.45m);

        register.Close(discrepancy, 7, "{\"cash\":123.45}", DateTime.UtcNow);

        Assert.False(register.IsOpen);
        Assert.Equal(123.45m, register.ClosingAmount);
        Assert.Equal(-1.55m, register.DifferenceAmount);
        Assert.True(register.IsImmutable);
        Assert.Throws<InvalidOperationException>(() => register.Close(discrepancy, 7, null, DateTime.UtcNow));
    }

    [Fact]
    public void FiscalDocument_ForSale_UsesCanonicalIdempotencyKey()
    {
        var document = FiscalDocument.ForSale(12, 34, "  E CF-1  ");

        Assert.Equal(12, document.CompanyId);
        Assert.Equal(34, document.SaleId);
        Assert.Equal("SALE:34", document.DocumentKey);
        Assert.Equal("E CF-1", document.Ncf);
        Assert.Equal(FiscalDocumentStatus.Pending, document.Status);
    }
}
