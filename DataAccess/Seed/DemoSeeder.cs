using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Onion.DataAccess.Repositories.Concrete;
using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain;
using Onion.Domain.Products;
using Onion.Domain.Users;
using BCrypt.Net;
using Microsoft.Extensions.DependencyInjection;

namespace Onion.DataAccess.Seed
{
    public static class DemoSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var uow = (IUnitOfWork)scope.ServiceProvider.GetRequiredService(typeof(IUnitOfWork));

            // Company
            var companyName = "Ferretería Demo SRL";
            var existingCompany = (await uow.Companies.FindAsync(c => c.Name == companyName)).FirstOrDefault();
            int companyId;
            if (existingCompany == null)
            {
                var comp = new Company { Name = companyName };
                await uow.Companies.AddAsync(comp);
                await uow.SaveChangesAsync();
                companyId = comp.Id;
            }
            else
            {
                companyId = existingCompany.Id;
            }

            // Admin user
            var adminEmail = "demo.admin@demo.local";
            var existingAdmin = (await uow.Users.FindAsync(u => u.Email == adminEmail)).FirstOrDefault();
            if (existingAdmin == null)
            {
                var admin = new Onion.Domain.Users.User
                {
                    Email = adminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoAdmin123!"),
                    FirstName = "Demo",
                    LastName = "Admin",
                    UserName = "demo.admin",
                    CompanyId = companyId,
                    Role = "Admin"
                };
                await uow.Users.AddAsync(admin);
                await uow.SaveChangesAsync();
            }

            // Products (idempotent by barcode)
            var products = new[] {
                new Product { Description = "Tornillo 1/4", Barcode = "P-0001", Cost = 0.50, Stock = 100, MinimumQuantity = 10, CompanyId = companyId },
                new Product { Description = "Martillo Acero 16oz", Barcode = "P-0002", Cost = 12.00, Stock = 5, MinimumQuantity = 10, CompanyId = companyId },
                new Product { Description = "Taladro 500W", Barcode = "P-0003", Cost = 45.00, Stock = 2, MinimumQuantity = 3, CompanyId = companyId },
                new Product { Description = "Llave inglesa 10mm", Barcode = "P-0004", Cost = 8.00, Stock = 20, MinimumQuantity = 5, CompanyId = companyId },
                new Product { Description = "Cinta métrica 5m", Barcode = "P-0005", Cost = 4.50, Stock = 0, MinimumQuantity = 2, CompanyId = companyId }
            };

            foreach (var p in products)
            {
                var exists = (await uow.Products.FindAsync(x => x.Barcode == p.Barcode && x.CompanyId == companyId)).FirstOrDefault();
                if (exists == null)
                {
                    await uow.Products.AddAsync(p);
                }
            }
            await uow.SaveChangesAsync();

            // Customers
            var customers = new[] {
                new Customer { Name = "Ferretería Central", Phone = "555-0101", Email = "central@example.com", CompanyId = companyId },
                new Customer { Name = "Constructora Uno", Phone = "555-0202", Email = "constru1@example.com", CompanyId = companyId },
                new Customer { Name = "Cliente Demo", Phone = "555-0303", Email = "cliente@example.com", CompanyId = companyId }
            };

            foreach (var c in customers)
            {
                var exists = (await uow.Customers.FindAsync(x => x.Email == c.Email && x.CompanyId == companyId)).FirstOrDefault();
                if (exists == null)
                {
                    await uow.Customers.AddAsync(c);
                }
            }
            await uow.SaveChangesAsync();

            // Sales: create a few example sales
            var productList = (await uow.Products.FindAsync(p => p.CompanyId == companyId)).ToList();
            var customerList = (await uow.Customers.FindAsync(c => c.CompanyId == companyId)).ToList();

            if ((await uow.Sales.FindAsync(s => s.CompanyId == companyId)).Count() < 3)
            {
                // Paid sale
                var sale1 = new Sale
                {
                    CompanyId = companyId,
                    CustomerId = customerList.FirstOrDefault()?.Id,
                    Total = 100m,
                    PaidAmount = 100m,
                    Status = SaleStatus.PAID,
                    PaymentType = PaymentType.CASH,
                    Details = new List<SaleDetail> { new SaleDetail { ProductId = productList[0].Id, Quantity = 2, UnitPrice = 25m } }
                };
                await uow.Sales.AddAsync(sale1);

                // Partial / pending
                var sale2 = new Sale
                {
                    CompanyId = companyId,
                    CustomerId = customerList.Skip(1).FirstOrDefault()?.Id,
                    Total = 200m,
                    PaidAmount = 50m,
                    Status = SaleStatus.PARTIAL,
                    PaymentType = PaymentType.CREDIT,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Details = new List<SaleDetail> { new SaleDetail { ProductId = productList[1].Id, Quantity = 1, UnitPrice = 200m } }
                };
                await uow.Sales.AddAsync(sale2);

                // Pending / unpaid
                var sale3 = new Sale
                {
                    CompanyId = companyId,
                    CustomerId = customerList.LastOrDefault()?.Id,
                    Total = 60m,
                    PaidAmount = 0m,
                    Status = SaleStatus.PENDING,
                    PaymentType = PaymentType.CREDIT,
                    DueDate = DateTime.UtcNow.AddDays(15),
                    Details = new List<SaleDetail> { new SaleDetail { ProductId = productList[2].Id, Quantity = 1, UnitPrice = 60m } }
                };
                await uow.Sales.AddAsync(sale3);

                await uow.SaveChangesAsync();
            }
        }
    }
}
