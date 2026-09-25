using Microsoft.EntityFrameworkCore.Storage;
using Onion.DataAccess;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using Onion.DataAccess.Repositories.Abstract;

namespace Onion.DataAccess.Repositories.Concrete
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IUserRepository Users { get; }
        ISaleRepository Sales { get; }
        IPurchaseRepository Purchases { get; }
        IRepository<Onion.Domain.Users.RefreshToken> RefreshTokens { get; }
        IRepository<Onion.Domain.Company> Companies { get; }
        IRepository<Onion.Domain.Invitations.Invitation> Invitations { get; }
        IRepository<Onion.Domain.Customer> Customers { get; }
        IRepository<Onion.Domain.Supplier> Suppliers { get; }
        IRepository<Onion.Domain.Payment> Payments { get; }
        IRepository<Onion.Domain.Return> Returns { get; }
        IRepository<Onion.Domain.CashRegister> CashRegisters { get; }
        IRepository<Onion.Domain.CashMovement> CashMovements { get; }
        IRepository<Onion.Domain.CashRegisterPause> CashRegisterPauses { get; }
        IRepository<Onion.Domain.CompanySettings> CompanySettingsRepo { get; }
        IRepository<Onion.Domain.Products.ProductType> ProductTypes { get; }
        IRepository<Onion.Domain.Inventory.InventoryMovement> InventoryMovements { get; }
        IRepository<Onion.Domain.Purchases.PurchaseOrderReceipt> PurchaseOrderReceipts { get; }
        IRepository<Onion.Domain.ManageRequests.ManageRequest> ManageRequests { get; }
        IRepository<Onion.Domain.Invoices.InvoiceSequence> InvoiceSequences { get; }
        IRepository<Onion.Domain.Invoices.FiscalDocument> FiscalDocuments { get; }
        IRepository<Onion.Domain.Finance.AccountReceivable> AccountReceivables { get; }
        IRepository<Onion.Domain.Finance.AccountPayable> AccountPayables { get; }
        IRepository<Onion.Domain.Finance.PaymentPlan> PaymentPlans { get; }
        IRepository<Onion.Domain.Finance.Installment> Installments { get; }
        IRepository<Onion.Domain.Billing.SubscriptionPlan> SubscriptionPlans { get; }
        IRepository<Onion.Domain.Billing.CompanySubscription> CompanySubscriptions { get; }
        IWarehouseRepository Warehouses { get; }
        IInventoryRepository Inventories { get; }
        IMovementRepository Movements { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly OnionDbContext _context;
        private readonly ILogger<UnitOfWork>? _logger;

        public IProductRepository Products { get; }
        public ICategoryRepository Categories { get; }
        public IUserRepository Users { get; }
        public ISaleRepository Sales { get; }
        public IPurchaseRepository Purchases { get; }
        public IRepository<Onion.Domain.Users.RefreshToken> RefreshTokens { get; }
        public IRepository<Onion.Domain.Company> Companies { get; }
        public IRepository<Onion.Domain.Invitations.Invitation> Invitations { get; }
        public IRepository<Onion.Domain.Customer> Customers { get; }
        public IRepository<Onion.Domain.Supplier> Suppliers { get; }
        public IRepository<Onion.Domain.Payment> Payments { get; }
        public IRepository<Onion.Domain.Return> Returns { get; }
        public IRepository<Onion.Domain.CashRegister> CashRegisters { get; }
        public IRepository<Onion.Domain.CashMovement> CashMovements { get; }
        public IRepository<Onion.Domain.CashRegisterPause> CashRegisterPauses { get; }
        public IRepository<Onion.Domain.CompanySettings> CompanySettingsRepo { get; }
        public IRepository<Onion.Domain.Products.ProductType> ProductTypes { get; }
        public IRepository<Onion.Domain.Inventory.InventoryMovement> InventoryMovements { get; }
        public IRepository<Onion.Domain.Purchases.PurchaseOrderReceipt> PurchaseOrderReceipts { get; }
        public IRepository<Onion.Domain.ManageRequests.ManageRequest> ManageRequests { get; }
        public IRepository<Onion.Domain.Invoices.InvoiceSequence> InvoiceSequences { get; }
        public IRepository<Onion.Domain.Invoices.FiscalDocument> FiscalDocuments { get; }
        public IRepository<Onion.Domain.Finance.AccountReceivable> AccountReceivables { get; }
        public IRepository<Onion.Domain.Finance.AccountPayable> AccountPayables { get; }
        public IRepository<Onion.Domain.Billing.SubscriptionPlan> SubscriptionPlans { get; }
        public IRepository<Onion.Domain.Billing.CompanySubscription> CompanySubscriptions { get; }
        public IWarehouseRepository Warehouses { get; }
        public IInventoryRepository Inventories { get; }
        public IMovementRepository Movements { get; }
        public IRepository<Onion.Domain.Finance.PaymentPlan> PaymentPlans { get; }
        public IRepository<Onion.Domain.Finance.Installment> Installments { get; }

        public UnitOfWork(OnionDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context;
            _logger = logger;
            Products = new ProductRepository(context);
            Categories = new CategoryRepository(context);
            Users = new UserRepository(context);
            Sales = new SaleRepository(context);
            Purchases = new PurchaseRepository(context);
            RefreshTokens = new GenericRepository<Onion.Domain.Users.RefreshToken>(context);
            Companies = new GenericRepository<Onion.Domain.Company>(context);
            Invitations = new GenericRepository<Onion.Domain.Invitations.Invitation>(context);
            Customers = new GenericRepository<Onion.Domain.Customer>(context);
            Suppliers = new GenericRepository<Onion.Domain.Supplier>(context);
            Payments = new GenericRepository<Onion.Domain.Payment>(context);
            Returns = new GenericRepository<Onion.Domain.Return>(context);
            CashRegisters = new GenericRepository<Onion.Domain.CashRegister>(context);
            CashMovements = new GenericRepository<Onion.Domain.CashMovement>(context);
            CashRegisterPauses = new GenericRepository<Onion.Domain.CashRegisterPause>(context);
            CompanySettingsRepo = new GenericRepository<Onion.Domain.CompanySettings>(context);
            ProductTypes = new GenericRepository<Onion.Domain.Products.ProductType>(context);
            InventoryMovements = new GenericRepository<Onion.Domain.Inventory.InventoryMovement>(context);
            PurchaseOrderReceipts = new GenericRepository<Onion.Domain.Purchases.PurchaseOrderReceipt>(context);
            ManageRequests = new GenericRepository<Onion.Domain.ManageRequests.ManageRequest>(context);
            InvoiceSequences = new GenericRepository<Onion.Domain.Invoices.InvoiceSequence>(context);
            FiscalDocuments = new GenericRepository<Onion.Domain.Invoices.FiscalDocument>(context);
            AccountReceivables = new GenericRepository<Onion.Domain.Finance.AccountReceivable>(context);
            AccountPayables = new GenericRepository<Onion.Domain.Finance.AccountPayable>(context);
            SubscriptionPlans = new GenericRepository<Onion.Domain.Billing.SubscriptionPlan>(context);
            CompanySubscriptions = new GenericRepository<Onion.Domain.Billing.CompanySubscription>(context);
            PaymentPlans = new GenericRepository<Onion.Domain.Finance.PaymentPlan>(context);
            Installments = new GenericRepository<Onion.Domain.Finance.Installment>(context);
            Warehouses = new WarehouseRepository(context);
            Inventories = new InventoryRepository(context);
            Movements = new MovementRepository(context);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }


        public async Task<int> SaveChangesAsync()
        {
            // Validate entities with DataAnnotations before saving to provide clear errors
            var validationErrors = new List<string>();
            var entries = _context.ChangeTracker.Entries().Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified).ToList();
            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(entity);
                var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                // Validate all properties and collect results
                if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(entity, validationContext, results, validateAllProperties: true))
                {
                    foreach (var r in results)
                    {
                        var memberNames = r.MemberNames != null && r.MemberNames.Any() ? string.Join(",", r.MemberNames) : entry.Entity.GetType().Name;
                        validationErrors.Add($"{memberNames}: {r.ErrorMessage}");
                    }
                }
            }

            if (validationErrors.Any())
            {
                var combined = string.Join("; ", validationErrors);
                _logger?.LogWarning("Model validation failed before SaveChanges: {Errors}", combined);
                // Create structured details per field to return to clients for better UX
                var details = new System.Collections.Generic.List<Onion.Common.Models.ValidationError>();
                foreach (var entry in entries)
                {
                    var entity = entry.Entity;
                    var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(entity);
                    var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    System.ComponentModel.DataAnnotations.Validator.TryValidateObject(entity, validationContext, results, validateAllProperties: true);
                    foreach (var r in results)
                    {
                        var field = r.MemberNames != null && r.MemberNames.Any() ? string.Join(",", r.MemberNames) : entity.GetType().Name;
                        details.Add(new Onion.Common.Models.ValidationError { Field = field, Message = r.ErrorMessage ?? string.Empty });
                    }
                }

                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "INVALID_MODEL", Message = "Validation failed", Language = "EN", Details = details });
            }

            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "DbUpdateException during SaveChangesAsync");
                // Map common SQL Server errors to friendly business error codes when possible
                var inner = dbEx.InnerException;
                if (inner is Microsoft.Data.SqlClient.SqlException sqlEx)
                {
                    switch (sqlEx.Number)
                    {
                        case 2627: // Unique constraint error
                        case 2601:
                            _logger?.LogWarning("Unique constraint violation: {Message}", sqlEx.Message);
                            throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "DUPLICATE_KEY", Message = "Duplicate value violates unique constraint.", Language = "EN" });
                        case 547: // Constraint check violation (FK)
                            _logger?.LogWarning("Foreign key constraint violation: {Message}", sqlEx.Message);
                            throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "FK_VIOLATION", Message = "Related entity not found or foreign key constraint violated.", Language = "EN" });
                        case 515: // Cannot insert the value NULL into column
                            _logger?.LogWarning("Null value insertion attempted: {Message}", sqlEx.Message);
                            throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NULL_VALUE", Message = "A required value was null. Check required fields.", Language = "EN" });
                        case 208: // Invalid object name (table missing)
                            _logger?.LogError(sqlEx, "Database table missing or migration required");
                            throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "TABLE_MISSING", Message = "Database table missing or migration required.", Language = "EN" });
                        default:
                            _logger?.LogError(sqlEx, "SQL error during SaveChanges");
                            throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = sqlEx.Message, Language = "EN" });
                    }
                }

                // Fallback for other providers or unknown inner exceptions
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "DB_ERROR", Message = dbEx.InnerException?.Message ?? dbEx.Message, Language = "EN" });
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
