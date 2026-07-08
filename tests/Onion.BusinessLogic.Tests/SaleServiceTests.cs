using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Onion.BussinesLogic.Services.Concrete;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain;
using Onion.DataAccess.Repositories.Abstract;
using System.Threading;

namespace Onion.BusinessLogic.Tests
{
    public class SaleServiceTests
    {
        [Fact]
        public async Task CreateAsync_WithSufficientStock_CompletesSale()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var productsMock = new Mock<IProductRepository>();
            var salesMock = new Mock<IRepository<Sale>>();
            var paymentsMock = new Mock<IRepository<Payment>>();
            var cashMovementsMock = new Mock<IRepository<CashMovement>>();

            // Setup product
            productsMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Product { Id = 1, Description = "P1", Stock = 10, InvoiceWithoutStock = false });
            productsMock.Setup(p => p.TryReserveStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).ReturnsAsync(true);
            productsMock.Setup(p => p.TryReduceStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).ReturnsAsync(true);

            // Setup unit of work
            uowMock.SetupGet(u => u.Products).Returns(productsMock.Object);
            uowMock.SetupGet(u => u.Sales).Returns(new Mock<IRepository<Sale>>().Object);
            uowMock.SetupGet(u => u.Payments).Returns(paymentsMock.Object);
            uowMock.SetupGet(u => u.CashMovements).Returns(cashMovementsMock.Object);
            uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SaleService>>();
            var svc = new SaleService(uowMock.Object, loggerMock.Object);

            var sale = new Sale
            {
                Details = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 2, UnitPrice = 5 } },
                PaidAmount = 10,
                Total = 10
            };

            await svc.CreateAsync(sale);

            productsMock.Verify(p => p.TryReserveStockAsync(1, 2), Times.AtLeastOnce);
            productsMock.Verify(p => p.TryReduceStockAsync(1, 2), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreateAsync_WithInsufficientStock_Throws()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var productsMock = new Mock<IProductRepository>();

            productsMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Product { Id = 1, Description = "P1", Stock = 1, InvoiceWithoutStock = false });
            productsMock.Setup(p => p.TryReserveStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).ReturnsAsync(false);

            uowMock.SetupGet(u => u.Products).Returns(productsMock.Object);
            uowMock.SetupGet(u => u.Sales).Returns(new Mock<IRepository<Sale>>().Object);
            uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SaleService>>();
            var svc = new SaleService(uowMock.Object, loggerMock.Object);

            var sale = new Sale
            {
                Details = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 2, UnitPrice = 5 } },
                PaidAmount = 0,
                Total = 10
            };

            await Assert.ThrowsAsync<Onion.Common.Exceptions.CustomException>(() => svc.CreateAsync(sale));
        }

        [Fact]
        public async Task AddPaymentAsync_UpdatesPaidAmountAndStatus()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var salesRepo = new Mock<IRepository<Sale>>();
            var paymentsRepo = new Mock<IRepository<Payment>>();

            var existingSale = new Sale { Id = 10, Total = 20m, PaidAmount = 5m, Status = SaleStatus.PARTIAL };

            salesRepo.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(existingSale);
            paymentsRepo.Setup(p => p.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask).Verifiable();

            uowMock.SetupGet(u => u.Sales).Returns(salesRepo.Object);
            uowMock.SetupGet(u => u.Payments).Returns(paymentsRepo.Object);
            uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1).Verifiable();

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SaleService>>();
            var svc = new SaleService(uowMock.Object, loggerMock.Object);

            var payment = new Payment { Amount = 15m, PaymentMethod = PaymentMethod.CASH };
            await svc.AddPaymentAsync(10, payment);

            paymentsRepo.Verify(p => p.AddAsync(It.Is<Payment>(x => x.Amount == 15m && x.SaleId == 10)), Times.Once);
            uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
            Assert.Equal(20m, existingSale.PaidAmount);
            Assert.Equal(SaleStatus.PAID, existingSale.Status);
        }

        [Fact]
        public async Task CreateAsync_WhenFinalizeFails_ReleasesReservations()
        {
            var uowMock = new Mock<IUnitOfWork>();
            var productsMock = new Mock<IProductRepository>();
            var salesMock = new Mock<IRepository<Sale>>();
            var cashMovementsMock = new Mock<IRepository<CashMovement>>();

            // Product exists
            productsMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(new Product { Id = 1, Description = "P1", Stock = 5, InvoiceWithoutStock = false });
            // Reserve succeeds
            productsMock.Setup(p => p.TryReserveStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).ReturnsAsync(true);
            // Final reduce fails to simulate concurrent stock loss
            productsMock.Setup(p => p.TryReduceStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).ReturnsAsync(false);
            productsMock.Setup(p => p.ReleaseReservedStockAsync(It.IsAny<int>(), It.IsAny<decimal>())).Returns(Task.CompletedTask).Verifiable();

            salesMock.Setup(s => s.AddAsync(It.IsAny<Sale>())).Returns(Task.CompletedTask);
            uowMock.SetupGet(u => u.Products).Returns(productsMock.Object);
            uowMock.SetupGet(u => u.Sales).Returns(salesMock.Object);
            uowMock.SetupGet(u => u.CashMovements).Returns(cashMovementsMock.Object);
            uowMock.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(Mock.Of<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());

            var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SaleService>>();
            var svc = new SaleService(uowMock.Object, loggerMock.Object);

            var sale = new Sale
            {
                Details = new List<SaleDetail> { new SaleDetail { ProductId = 1, Quantity = 2, UnitPrice = 5 } },
                PaidAmount = 0,
                Total = 10
            };

            await Assert.ThrowsAsync<Onion.Common.Exceptions.CustomException>(() => svc.CreateAsync(sale));

            // Ensure release was called
            productsMock.Verify(p => p.ReleaseReservedStockAsync(1, 2), Times.AtLeastOnce);
        }
    }
}
