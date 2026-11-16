using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onion.BussinesLogic.Services.Concrete;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.DataExchange;
using Onion.Domain.Products;
using Xunit;

namespace Onion.Tests
{
    public sealed class DataExchangeServiceTests
    {
        private sealed class TestTenantContext : ICurrentUserService
        {
            public int? CompanyId => 1;
            public int? UserId => 42;
            public string? ClerkUserId => "user_42";
            public string? UserEmail => "admin@cuadreenv.com";
            public string? UserRole => "Admin";
            public string? UserPermissions => "ALL";
            public string? IpAddress => "127.0.0.1";
            public string? UserAgent => "TestRunner";
            public bool IsAuthenticated => true;
            public bool IsGlobalAdministrator => false;
        }

        private OnionDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<OnionDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new OnionDbContext(options, currentUserService: new TestTenantContext());
        }

        [Fact]
        public async Task GenerateTemplateAsync_Products_ReturnsValidExcelFile()
        {
            await using var db = CreateDbContext();
            var unitOfWork = new UnitOfWork(db);
            var logger = NullLogger<DataExchangeService>.Instance;
            var service = new DataExchangeService(unitOfWork, logger);

            var (content, contentType, fileName) = await service.GenerateTemplateAsync(1, "products", "xlsx");

            Assert.NotNull(content);
            Assert.True(content.Length > 0);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", contentType);
            Assert.Contains("plantilla_productos.xlsx", fileName);
        }

        [Fact]
        public async Task PreviewAndConfirmImport_UpdatesExistingAndIdempotent()
        {
            await using var db = CreateDbContext();

            // Seed initial category, type, and product
            var cat = new Category { Id = 1, CompanyId = 1, Description = "Bebidas", Active = true, IsDeleted = false };
            var ptype = new ProductType { Id = 1, CompanyId = 1, Description = "Físico", Active = true, IsDeleted = false };
            var prod = new Product
            {
                Id = 100,
                CompanyId = 1,
                Reference = "REF-001",
                Barcode = "746000000001",
                Description = "Refresco Cola 500ml",
                Cost = 25.00m,
                Price = 40.00m,
                Stock = 50,
                MinimumQuantity = 5,
                CategoryId = 1,
                ProductTypeId = 1,
                UnitOfMeasurementId = 1,
                Active = true,
                IsDeleted = false
            };
            db.Categories.Add(cat);
            db.ProductTypes.Add(ptype);
            db.Products.Add(prod);
            await db.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(db);
            var logger = NullLogger<DataExchangeService>.Instance;
            var service = new DataExchangeService(unitOfWork, logger);

            // Prepare CSV changing price to 55.00, cost empty (should preserve 25.00), and adding 1 new product
            var csv = new StringBuilder();
            csv.AppendLine("Referencia,Codigo_Barra,Descripcion,Categoria,Costo_Compra,Precio_Venta,Stock_Inicial,Stock_Minimo");
            csv.AppendLine("REF-001,746000000001,Refresco Cola 500ml,, ,55.00, , ");
            csv.AppendLine("REF-002,746000000002,Jugo Naranja 1L,Bebidas,30.00,60.00,20,5");

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv.ToString()));

            // 1. Preview
            var preview = await service.PreviewImportAsync(1, "products", stream, "test.csv", ImportMode.CreateAndUpdate, "user_42");

            Assert.NotNull(preview);
            Assert.Equal(2, preview.TotalRows);
            Assert.Equal(1, preview.RowsToUpdate);
            Assert.Equal(1, preview.RowsToInsert);
            Assert.Equal(0, preview.RowsWithErrors);
            Assert.True(preview.CanConfirm);

            // 2. First Confirm
            var confirm1 = await service.ConfirmImportAsync(1, preview.BatchId, "user_42");
            Assert.True(confirm1.Success);
            Assert.False(confirm1.AlreadyApplied);
            Assert.Equal(1, confirm1.UpdatedCount);
            Assert.Equal(1, confirm1.InsertedCount);

            // Verify in DB that price updated to 55 and cost was preserved at 25
            var updatedProd = await db.Products.FirstAsync(p => p.Reference == "REF-001");
            Assert.Equal(55.00m, updatedProd.Price);
            Assert.Equal(25.00m, updatedProd.Cost);

            // Verify new product was inserted
            var newProd = await db.Products.FirstOrDefaultAsync(p => p.Reference == "REF-002");
            Assert.NotNull(newProd);
            Assert.Equal(60.00m, newProd.Price);

            // 3. Second Confirm (Idempotency test)
            var confirm2 = await service.ConfirmImportAsync(1, preview.BatchId, "user_42");
            Assert.True(confirm2.Success);
            Assert.True(confirm2.AlreadyApplied);
        }
    }
}
