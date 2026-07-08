using Microsoft.EntityFrameworkCore.Storage;
using Onion.DataAccess;
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
        IRepository<Onion.Domain.CompanySettings> CompanySettingsRepo { get; }
        IRepository<Onion.Domain.Products.ProductType> ProductTypes { get; }
        IWarehouseRepository Warehouses { get; }
        IInventoryRepository Inventories { get; }
        IMovementRepository Movements { get; }

        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly OnionDbContext _context;

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
        public IRepository<Onion.Domain.CompanySettings> CompanySettingsRepo { get; }
        public IRepository<Onion.Domain.Products.ProductType> ProductTypes { get; }
        public IWarehouseRepository Warehouses { get; }
        public IInventoryRepository Inventories { get; }
        public IMovementRepository Movements { get; }

        public UnitOfWork(OnionDbContext context)
        {
            _context = context;
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
            CompanySettingsRepo = new GenericRepository<Onion.Domain.CompanySettings>(context);
            ProductTypes = new GenericRepository<Onion.Domain.Products.ProductType>(context);
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
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
