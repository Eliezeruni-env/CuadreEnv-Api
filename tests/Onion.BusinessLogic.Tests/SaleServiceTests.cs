using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Services.Concrete;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;
using Xunit;

namespace Onion.BusinessLogic.Tests;

public sealed class SaleServiceTests
{
    [Fact]
    public async Task CreateAsync_WithoutDetails_RejectsSale()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
        var service = CreateService(uow);

        var exception = await Assert.ThrowsAsync<Onion.Common.Exceptions.CustomException>(() =>
            service.CreateAsync(new Sale { CompanyId = 1 }));

        Assert.Equal("NO_ITEMS", exception.Error.Code);
        uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithClosedCashRegister_RejectsSaleBeforeTransaction()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
        uow.Setup(x => x.CashRegisters.GetByIdAsync(10)).ReturnsAsync(new CashRegister
        {
            Id = 10,
            CompanyId = 1,
            Status = CashRegisterStatus.CLOSED,
            IsOpen = false
        });
        var service = CreateService(uow);

        var exception = await Assert.ThrowsAsync<Onion.Common.Exceptions.CustomException>(() =>
            service.CreateAsync(new Sale
            {
                CompanyId = 1,
                CashRegisterId = 10,
                Details = [new SaleDetail { ProductId = 20, Quantity = 1, UnitPrice = 10 }]
            }));

        Assert.Equal("CASH_REGISTER_NOT_OPEN", exception.Error.Code);
        uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithExistingIdempotencyKey_ReturnsExistingSaleWithoutMutation()
    {
        var uow = new Mock<IUnitOfWork>(MockBehavior.Loose);
        var existing = new Sale { Id = 55, CompanyId = 1, IdempotencyKey = "sale-55" };
        uow.Setup(x => x.Sales.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sale, bool>>>()))
            .ReturnsAsync([existing]);
        var service = CreateService(uow);

        var result = await service.CreateAsync(new Sale
        {
            CompanyId = 1,
            IdempotencyKey = "  sale-55  ",
            Details = [new SaleDetail { ProductId = 20, Quantity = 1, UnitPrice = 10 }]
        });

        Assert.Same(existing, result);
        uow.Verify(x => x.BeginTransactionAsync(), Times.Never);
        uow.Verify(x => x.Sales.AddAsync(It.IsAny<Sale>()), Times.Never);
    }

    private static SaleService CreateService(Mock<IUnitOfWork> uow)
    {
        var pagination = new Mock<IPaginationService>();
        return new SaleService(uow.Object, NullLogger<SaleService>.Instance, pagination.Object);
    }
}