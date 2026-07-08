using Microsoft.EntityFrameworkCore;
using Onion.Domain;
using Onion.Domain.Products;
using Onion.Domain.Users;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.DataAccess
{
    public interface ITenantProvider
    {
        int? GetCompanyId();
    }

    public class OnionDbContext : DbContext
    {
        private readonly ITenantProvider? _tenantProvider;
        // Expose tenant id as a property so EF Core query filters can reference the DbContext instance
        // This property will be evaluated at query time via the DbContext instance (avoids capturing a single value at model build time)
        public int? TenantCompanyId => _tenantProvider?.GetCompanyId();

        public OnionDbContext(DbContextOptions<OnionDbContext> options, ITenantProvider? tenantProvider = null)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<Purchase> Purchases { get; set; } = null!;
        public DbSet<PurchaseDetail> PurchaseDetails { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleDetail> SaleDetails { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Return> Returns { get; set; } = null!;
        public DbSet<ReturnDetail> ReturnDetails { get; set; } = null!;
        public DbSet<CashRegister> CashRegisters { get; set; } = null!;
        public DbSet<CashMovement> CashMovements { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Onion.Domain.Users.RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Onion.Domain.Invitations.Invitation> Invitations { get; set; } = null!;
        public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Warehouse> Warehouses { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Inventory> Inventories { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Movement> Movements { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.Settings)
                .WithOne(s => s.Company)
                .HasForeignKey<CompanySettings>(s => s.CompanyId);

            // Use the DbContext property 'TenantCompanyId' so the filter reads tenant from the provider at query time.
            // Use null-check comparison to avoid accessing .Value in EF translation; compare nullable CompanyId to TenantCompanyId directly
            modelBuilder.Entity<Category>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Product>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Customer>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Purchase>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Sale>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<Return>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<CashRegister>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            modelBuilder.Entity<CashMovement>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);
            // Do not apply tenant filter to users to allow cross-company authentication and administration.
            modelBuilder.Entity<CompanySettings>().HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);

            // Ensure tenant query filters are applied for warehouse-related and invitation entities
            modelBuilder.Entity<Onion.Domain.Warehouses.Warehouse>()
                .HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);

            modelBuilder.Entity<Onion.Domain.Warehouses.Inventory>()
                .HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);

            modelBuilder.Entity<Onion.Domain.Warehouses.Movement>()
                .HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);

            // Invitation entity exists under Domain.Invitations and must be tenant-scoped
            modelBuilder.Entity<Onion.Domain.Invitations.Invitation>()
                .HasQueryFilter(e => this.TenantCompanyId == null || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyId);

            modelBuilder.Entity<Onion.Domain.Users.RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is BaseEntity &&  (e.State == EntityState.Added || e.State == EntityState.Modified)))
            {
                var entity = (BaseEntity)entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    entity.CreationDate = now;
                    // Set CompanyId from tenant provider when adding new entities
                    if (_tenantProvider != null)
                    {
                        var companyId = _tenantProvider.GetCompanyId();
                        if (companyId.HasValue)
                        {
                            // Only set property if entity has CompanyId
                            var prop = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "CompanyId", StringComparison.OrdinalIgnoreCase));
                            if (prop != null && (prop.CurrentValue == null || (int)prop.CurrentValue == 0))
                                prop.CurrentValue = companyId.Value;
                        }
                    }
                }
                else
                {
                    entity.ModificationDate = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
