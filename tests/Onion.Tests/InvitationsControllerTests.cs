using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.Common.Authorization;
using Onion.Controllers;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using System.Linq;
using System;
using Onion.Domain.Invitations;
using Onion.Common.Services;
using Xunit;
using System.Threading.Tasks;

namespace Onion.Tests
{
    public class InvitationsControllerTests
    {
        private ClaimsPrincipal CreateUser(string? role = null, int? companyId = null)
        {
            var claims = new System.Collections.Generic.List<Claim>();
            if (role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }
            if (companyId.HasValue)
            {
                claims.Add(new Claim("CompanyId", companyId.Value.ToString()));
            }
            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        private class FakeUow : Onion.DataAccess.Repositories.Concrete.IUnitOfWork
        {
            public System.Threading.Tasks.Task<int> SaveChangesAsync() => System.Threading.Tasks.Task.FromResult(0);
            public System.Threading.Tasks.Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync() => throw new System.NotImplementedException();
            public void Dispose() { }

            // Only the Invitations repository is used by controller in this test
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Invitations.Invitation> Invitations { get; } = new FakeInvRepo();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Purchases.PurchaseOrderReceipt> PurchaseOrderReceipts => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.ManageRequests.ManageRequest> ManageRequests => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Finance.PaymentPlan> PaymentPlans => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Finance.Installment> Installments => throw new System.NotImplementedException();

            // Unused members - implement with throw to satisfy interface
            public Onion.DataAccess.Repositories.Abstract.IProductRepository Products => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.ICategoryRepository Categories => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IUserRepository Users => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.ISaleRepository Sales => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IPurchaseRepository Purchases => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Users.RefreshToken> RefreshTokens => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Company> Companies => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Customer> Customers => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Supplier> Suppliers => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Payment> Payments => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Return> Returns => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.CashRegister> CashRegisters => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.CashMovement> CashMovements => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.CashRegisterPause> CashRegisterPauses => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.CompanySettings> CompanySettingsRepo => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Products.ProductType> ProductTypes => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Inventory.InventoryMovement> InventoryMovements => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Invoices.InvoiceSequence> InvoiceSequences => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Invoices.FiscalDocument> FiscalDocuments => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Finance.AccountReceivable> AccountReceivables => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Finance.AccountPayable> AccountPayables => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Billing.SubscriptionPlan> SubscriptionPlans => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Billing.CompanySubscription> CompanySubscriptions => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IWarehouseRepository Warehouses => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IInventoryRepository Inventories => throw new System.NotImplementedException();
            public Onion.DataAccess.Repositories.Abstract.IMovementRepository Movements => throw new System.NotImplementedException();
        }

        private class FakeInvRepo : Onion.DataAccess.Repositories.Abstract.IRepository<Onion.Domain.Invitations.Invitation>
        {
            private readonly System.Collections.Generic.List<Invitation> _items = new();
            public System.Threading.Tasks.Task AddAsync(Invitation entity)
            {
                _items.Add(entity);
                return System.Threading.Tasks.Task.CompletedTask;
            }
            public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Invitation>> FindAsync(System.Linq.Expressions.Expression<System.Func<Invitation, bool>> predicate)
            {
                var compiled = predicate.Compile();
                return System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IEnumerable<Invitation>>(_items.Where(compiled));
            }
            public System.Threading.Tasks.Task<Invitation?> GetByIdAsync(int id) => System.Threading.Tasks.Task.FromResult<Invitation?>(null);
            public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Invitation>> ListAsync() => System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IEnumerable<Invitation>>(_items);
            public void Remove(Invitation entity) => _items.Remove(entity);
            public void Update(Invitation entity) { }
            public System.Threading.Tasks.Task<Onion.Common.Models.Pagination.PagedList<Invitation>> GetPagedAsync(int pageNumber, int pageSize) => throw new System.NotImplementedException();
            public System.Threading.Tasks.Task<Onion.Common.Models.Pagination.PagedList<Invitation>> GetPagedAsync(System.Linq.IQueryable<Invitation> query, int pageNumber, int pageSize) => throw new System.NotImplementedException();
        }

        [Fact]
        public async Task Create_BadRequest_When_CompanyId_Missing()
        {
            var uow = new FakeUow();
            var ctrl = new Onion.Controllers.InvitationsController(uow, null, null, new AuthorizationService());
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() } };

            var result = await ctrl.Create(new InvitationRequest("a@b.com"));
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Ok_When_CompanyId_Present_And_Admin()
        {
            var uow = new FakeUow();
            var ctrl = new Onion.Controllers.InvitationsController(uow, null, null, new AuthorizationService());
            var user = CreateUser("Admin", 5);
            ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

            var result = await ctrl.Create(new InvitationRequest("a@b.com"));
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
