using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Onion.Domain;
using Onion.Domain.Products;
using Onion.Domain.Users;
using Onion.Domain.Authorization;
using Onion.DataAccess.Clerk;

namespace Onion.DataAccess
{
    public interface ITenantProvider
    {
        int? GetCompanyId();
    }

    public class OnionDbContext : DbContext
    {
        private readonly ITenantProvider? _tenantProvider;
        private readonly Onion.Common.Services.ICurrentUserService? _currentUserService;
        private readonly IOrganizationTenantProvider? _organizationTenantProvider;
        // Expose tenant id as a property so EF Core query filters can reference the DbContext instance
        // This property will be evaluated at query time via the DbContext instance (avoids capturing a single value at model build time)
        // Prefer ICurrentUserService (reads claims) and fall back to ITenantProvider for design-time scenarios.
        // Also check the static AmbientTenantProvider.CurrentCompanyId as a final fallback for background jobs
        // where the ITenantProvider (e.g., JwtTenantProvider) is not applicable.
        public int? TenantCompanyId
        {
            get
            {
                // If an ambient bypass flag is set (development admin override), do not apply tenant filtering
                try
                {
                    if (Onion.DataAccess.Tenant.AmbientTenantProvider.BypassTenant)
                        return null;
                }
                catch
                {
                    // ignore errors reading ambient provider
                }

                var userCompany = _currentUserService?.CompanyId;
                if (userCompany.HasValue) return userCompany;

                if (_tenantProvider is Onion.DataAccess.Tenant.AmbientTenantProvider ambient)
                {
                    var amb = ambient.GetCompanyId();
                    if (amb.HasValue) return amb;
                }

                var tp = _tenantProvider?.GetCompanyId();
                if (tp.HasValue) return tp;

                // Last resort: static ambient override set by background jobs
                return Onion.DataAccess.Tenant.AmbientTenantProvider.CurrentCompanyId;
            }
        }

        public bool IsGlobalTenantAccess => _currentUserService?.IsGlobalAdministrator == true;
        // Projects currently reuse the legacy OrganizationId column. Local JWTs
        // scope it using CompanyId without requiring Clerk organization claims.
        public string TenantOrganizationIdForQuery => TenantCompanyId?.ToString() ?? "__no_company__";

        [NotMapped]
        public int? TenantCompanyIdForQuery => IsGlobalTenantAccess ? null : TenantCompanyId ?? -1;

        public OnionDbContext(DbContextOptions<OnionDbContext> options, ITenantProvider? tenantProvider = null, Onion.Common.Services.ICurrentUserService? currentUserService = null, IOrganizationTenantProvider? organizationTenantProvider = null)
            : base(options)
        {
            _tenantProvider = tenantProvider;
            _currentUserService = currentUserService;
            _organizationTenantProvider = organizationTenantProvider;
        }



        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Onion.Domain.Products.ProductType> ProductTypes { get; set; } = null!;
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
        public DbSet<CashRegisterPause> CashRegisterPauses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Onion.Domain.Users.RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<Onion.Domain.Invitations.Invitation> Invitations { get; set; } = null!;
        public DbSet<CompanySettings> CompanySettings { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Warehouse> Warehouses { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Inventory> Inventories { get; set; } = null!;
        public DbSet<Onion.Domain.Warehouses.Movement> Movements { get; set; } = null!;
        public DbSet<Onion.Domain.Inventory.InventoryMovement> InventoryMovements { get; set; } = null!;
        public DbSet<Onion.Domain.Invoices.InvoiceSequence> InvoiceSequences { get; set; } = null!;
        public DbSet<Onion.Domain.Invoices.FiscalDocument> FiscalDocuments { get; set; } = null!;
        public DbSet<Onion.Domain.Invoices.FiscalSubmissionAudit> FiscalSubmissionAudits { get; set; } = null!;
        public DbSet<Onion.Domain.Audit.AuditLog> AuditLogs { get; set; } = null!;
        // Purchase receipts and manage requests
        public DbSet<Onion.Domain.Purchases.PurchaseOrderReceipt> PurchaseOrderReceipts { get; set; } = null!;
        public DbSet<Onion.Domain.Purchases.PurchaseOrderReceiptDetail> PurchaseOrderReceiptDetails { get; set; } = null!;
        public DbSet<Onion.Domain.ManageRequests.ManageRequest> ManageRequests { get; set; } = null!;
        public DbSet<Onion.Domain.ManageRequests.ManageRequestTimeline> ManageRequestTimelines { get; set; } = null!;
        public DbSet<Onion.Domain.Finance.AccountReceivable> AccountReceivables { get; set; } = null!;
        public DbSet<Onion.Domain.Finance.AccountPayable> AccountPayables { get; set; } = null!;
        public DbSet<Onion.Domain.Finance.PaymentPlan> PaymentPlans { get; set; } = null!;
        public DbSet<Onion.Domain.Finance.Installment> Installments { get; set; } = null!;
        public DbSet<Onion.Domain.Billing.SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<Onion.Domain.Billing.CompanySubscription> CompanySubscriptions { get; set; } = null!;
        // Credits module
        public DbSet<Onion.Domain.Credits.Credit> Credits { get; set; } = null!;
        public DbSet<Onion.Domain.Credits.CreditPayment> CreditPayments { get; set; } = null!;
        public DbSet<Onion.Domain.Credits.CreditStatusHistory> CreditStatusHistory { get; set; } = null!;
        // Appointments module
        public DbSet<Onion.Domain.Appointments.Appointment> Appointments { get; set; } = null!;
        public DbSet<Onion.Domain.Appointments.Resource> Resources { get; set; } = null!;
        public DbSet<Onion.Domain.Appointments.Availability> Availabilities { get; set; } = null!;
        // Credit notes
        public DbSet<Onion.Domain.CreditNote> CreditNotes { get; set; } = null!;
        public DbSet<Onion.Domain.CreditNoteDetail> CreditNoteDetails { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<DeletionApprovalRequest> DeletionApprovalRequests { get; set; } = null!;
        public DbSet<ClerkUser> ClerkUsers { get; set; } = null!;
        public DbSet<ClerkOrganization> ClerkOrganizations { get; set; } = null!;
        public DbSet<ClerkOrganizationMember> ClerkOrganizationMembers { get; set; } = null!;
        public DbSet<Proyecto> Proyectos { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClerkUser>(entity =>
            {
                entity.HasKey(e => e.ClerkUserId);
                entity.Property(e => e.ClerkUserId).HasMaxLength(64);
                entity.Property(e => e.Email).HasMaxLength(320);
                entity.Property(e => e.FirstName).HasMaxLength(150);
                entity.Property(e => e.LastName).HasMaxLength(150);
                entity.Property(e => e.ImageUrl).HasMaxLength(2048);
            });

            modelBuilder.Entity<ClerkOrganization>(entity =>
            {
                entity.HasKey(e => e.ClerkOrganizationId);
                entity.Property(e => e.ClerkOrganizationId).HasMaxLength(64);
                entity.Property(e => e.Name).HasMaxLength(250).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(250);
            });

            modelBuilder.Entity<ClerkOrganizationMember>(entity =>
            {
                entity.HasKey(e => new { e.ClerkUserId, e.ClerkOrganizationId });
                entity.Property(e => e.ClerkUserId).HasMaxLength(64);
                entity.Property(e => e.ClerkOrganizationId).HasMaxLength(64);
                entity.Property(e => e.Role).HasMaxLength(100).IsRequired();
                entity.HasOne(e => e.User).WithMany(e => e.OrganizationMemberships)
                    .HasForeignKey(e => e.ClerkUserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Organization).WithMany(e => e.Members)
                    .HasForeignKey(e => e.ClerkOrganizationId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.ClerkOrganizationId);
            });

            modelBuilder.Entity<Proyecto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).HasMaxLength(64).IsRequired();
                entity.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Descripcion).HasMaxLength(2000);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasOne(e => e.Organization).WithMany(e => e.Proyectos)
                    .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
                 entity.HasQueryFilter(e => IsGlobalTenantAccess || e.OrganizationId == TenantOrganizationIdForQuery);
            });

            modelBuilder.Entity<Company>()
                .HasOne(c => c.Settings)
                .WithOne(s => s.Company)
                .HasForeignKey<CompanySettings>(s => s.CompanyId);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(e => new { e.CompanyId, e.Stock, e.MinimumQuantity });
            });

            modelBuilder.Entity<CashRegister>(entity =>
            {
                entity.Property(e => e.OpeningAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.InitialAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ExpectedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PhysicalCountAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.DifferenceAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PhysicalCountBreakdownJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(e => new { e.CompanyId, e.Status });
            });

            modelBuilder.Entity<CashMovement>(entity =>
            {
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
                entity.Property(e => e.RecordedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
                entity.HasIndex(e => new { e.CompanyId, e.CashRegisterId, e.RecordedAt });
            });

            modelBuilder.Entity<Onion.Domain.Invoices.InvoiceSequence>(entity =>
            {
                entity.HasIndex(e => new { e.CompanyId }).IsUnique();
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasIndex(e => new { e.CompanyId, e.IdempotencyKey, e.IsDeleted })
                    .IsUnique()
                    .HasFilter("[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");
                entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken();
            });

            modelBuilder.Entity<Onion.Domain.Invoices.FiscalDocument>(entity =>
            {
                entity.Property(e => e.DocumentKey).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Ncf).HasMaxLength(30);
                entity.Property(e => e.EcfTrackId).HasMaxLength(200);
                entity.HasIndex(e => new { e.CompanyId, e.DocumentKey }).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.Status, e.NextAttemptAt });
            });
            modelBuilder.Entity<Onion.Domain.Invoices.FiscalSubmissionAudit>(entity =>
            {
                entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.PayloadHash).HasMaxLength(128);
                entity.HasIndex(e => new { e.CompanyId, e.FiscalDocumentId, e.OccurredAt });
            });


            // Use the DbContext property 'TenantCompanyId' so the filter reads tenant from the provider at query time.
            // Use null-check comparison to avoid accessing .Value in EF translation; compare nullable CompanyId to TenantCompanyId directly
            modelBuilder.Entity<Category>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Product>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Onion.Domain.Products.ProductType>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Onion.Domain.CreditNote>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Onion.Domain.CreditNoteDetail>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            // Seed default product types to allow FE to create products referencing common types
            // Use fixed seed CreationDate values to avoid non-deterministic migrations
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Onion.Domain.Products.ProductType>().HasData(
                new { Id = 1, Description = "Producto Est�ndar", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 2, Description = "Servicio", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 3, Description = "Digital", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 4, Description = "Combo", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 5, Description = "Materia Prima", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" }
            );
            // Seed default categories so FE has values to choose from
            modelBuilder.Entity<Onion.Domain.Products.Category>().HasData(
                new { Id = 1, Description = "General", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 2, Description = "Alimentos", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 3, Description = "Bebidas", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 4, Description = "Papeler�a", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" },
                new { Id = 5, Description = "Servicios", CompanyId = 0, CreationDate = seedDate, Active = true, IsDeleted = false, ModificationDate = (DateTime?)null, CreateBy = "system", ModifiedBy = "system" }
            );
            modelBuilder.Entity<Customer>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Purchase>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Sale>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<Return>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<CashRegister>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<CashMovement>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted && (this.IsGlobalTenantAccess || e.CompanyId == this.TenantCompanyIdForQuery));
            modelBuilder.Entity<Onion.Domain.Audit.AuditLog>().HasQueryFilter(e => this.IsGlobalTenantAccess || e.CompanyId == this.TenantCompanyIdForQuery);
            modelBuilder.Entity<CompanySettings>().HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique();
                entity.HasQueryFilter(e => this.IsGlobalTenantAccess || e.CompanyId == this.TenantCompanyIdForQuery);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Module).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
                entity.HasIndex(e => new { e.Module, e.Action }).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => new { e.RoleId, e.PermissionId });
                entity.HasOne(e => e.Role).WithMany(e => e.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Permission).WithMany(e => e.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role).WithMany(e => e.UserRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasQueryFilter(e => this.IsGlobalTenantAccess || e.CompanyId == this.TenantCompanyIdForQuery);
                entity.HasIndex(e => new { e.CompanyId, e.UserId });
                entity.HasIndex(e => new { e.CompanyId, e.RoleId });
            });

            modelBuilder.Entity<DeletionApprovalRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityType).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(30).IsRequired();
                entity.HasIndex(e => new { e.CompanyId, e.Status });
                entity.HasQueryFilter(e => this.IsGlobalTenantAccess || e.CompanyId == this.TenantCompanyIdForQuery);
            });

            // Ensure tenant query filters are applied for warehouse-related and invitation entities
            modelBuilder.Entity<Onion.Domain.Warehouses.Warehouse>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Warehouses.Inventory>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Warehouses.Movement>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Inventory.InventoryMovement>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Invoices.InvoiceSequence>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Finance.AccountReceivable>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Finance.AccountPayable>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Billing.CompanySubscription>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            // Credits tenant filters
            modelBuilder.Entity<Onion.Domain.Credits.Credit>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Credits.CreditPayment>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Credits.CreditStatusHistory>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            // Configure relationships and indexes for Credits module
            modelBuilder.Entity<Onion.Domain.Credits.Credit>(eb =>
            {
                eb.HasMany(e => e.Payments)
                  .WithOne(p => p.Credit)
                  .HasForeignKey(p => p.CreditId)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasMany(e => e.StatusHistory)
                  .WithOne(h => h.Credit)
                  .HasForeignKey(h => h.CreditId)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasIndex("CompanyId");
            });

            modelBuilder.Entity<Onion.Domain.Credits.CreditPayment>(eb =>
            {
                eb.HasIndex(p => p.CreditId);
                eb.HasIndex("CompanyId");
            });

            modelBuilder.Entity<Onion.Domain.Credits.CreditStatusHistory>(eb =>
            {
                eb.HasIndex(h => h.CreditId);
                eb.HasIndex("CompanyId");
            });

            // Appointment module tenant filters
            modelBuilder.Entity<Onion.Domain.Appointments.Appointment>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Appointments.Resource>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            modelBuilder.Entity<Onion.Domain.Appointments.Availability>()
                .HasQueryFilter(e => this.IsGlobalTenantAccess || EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            // Resource-Availability relationship
            modelBuilder.Entity<Onion.Domain.Appointments.Resource>(eb =>
            {
                eb.HasMany(r => r.Availabilities)
                  .WithOne(a => a.Resource)
                  .HasForeignKey(a => a.ResourceId)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasIndex("CompanyId");
            });

            modelBuilder.Entity<Onion.Domain.Appointments.Availability>(eb =>
            {
                eb.HasIndex(a => a.ResourceId);
                eb.HasIndex("CompanyId");
            });

            modelBuilder.Entity<Onion.Domain.Appointments.Appointment>(eb =>
            {
                // Optional relationship to Resource
                eb.HasOne<Onion.Domain.Appointments.Resource>()
                  .WithMany()
                  .HasForeignKey("ResourceId")
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);

                eb.HasIndex("CompanyId");
            });

            // Invitation entity exists under Domain.Invitations and must be tenant-scoped
            modelBuilder.Entity<Onion.Domain.Invitations.Invitation>()
                .HasQueryFilter(e => EF.Property<int?>(e, "CompanyId") == this.TenantCompanyIdForQuery);

            // AccountReceivable relationships
            modelBuilder.Entity<Onion.Domain.Finance.PaymentPlan>()
                .HasMany(p => p.Installments)
                .WithOne()
                .HasForeignKey("PaymentPlanId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Onion.Domain.Finance.Installment>()
                .Property(i => i.Status)
                .HasConversion<int>();

            modelBuilder.Entity<Onion.Domain.Users.RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var auditEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && e.Entity is not Onion.Domain.Audit.AuditLog &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();
            var currentUser = _currentUserService;

            foreach (var entry in auditEntries)
            {
                var entity = (BaseEntity)entry.Entity;
                var changes = new Dictionary<string, object?>();
                foreach (var property in entry.Properties)
                {
                    if (entry.State == EntityState.Added)
                        changes[property.Metadata.Name] = new { Old = (object?)null, New = property.CurrentValue };
                    else if (entry.State == EntityState.Deleted)
                        changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = (object?)null };
                    else if (!Equals(property.OriginalValue, property.CurrentValue))
                        changes[property.Metadata.Name] = new { Old = property.OriginalValue, New = property.CurrentValue };
                }

                if (entry.State == EntityState.Added)
                {
                    entity.CreationDate = now;
                    // Set CompanyId from the effective tenant source (ICurrentUserService or AmbientTenantProvider or ITenantProvider fallback)
                    var tenantId = this.TenantCompanyId;
                    if (tenantId.HasValue)
                    {
                        // If the entity exposes a CompanyId property (tenant-scoped by convention), set it from the effective tenant
                        var prop = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "CompanyId", StringComparison.OrdinalIgnoreCase));
                        if (prop != null && (prop.CurrentValue == null || (int)prop.CurrentValue == 0))
                            prop.CurrentValue = tenantId.Value;
                    }
                    else
                    {
                        // When adding tenant-scoped entities (entities that have a CompanyId property), tenant must be present
                        var hasCompanyProp = entry.Properties.Any(p => string.Equals(p.Metadata.Name, "CompanyId", StringComparison.OrdinalIgnoreCase));
                        var isUnassignedUser = entry.Entity is User user && !user.CompanyId.HasValue;
                        if (hasCompanyProp && !isUnassignedUser)
                            throw new InvalidOperationException("Tenant company id missing for multi-tenant operation.");
                    }
                }
                else
                {
                    entity.ModificationDate = now;
                }

                var companyProperty = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "CompanyId", StringComparison.OrdinalIgnoreCase));
                var entityName = entry.Metadata.ClrType.Name;
                AuditLogs.Add(new Onion.Domain.Audit.AuditLog
                {
                    CompanyId = companyProperty?.CurrentValue as int? ?? (companyProperty?.OriginalValue as int?),
                    UserId = currentUser?.UserId,
                    UserEmail = currentUser?.UserEmail,
                    UserRole = currentUser?.UserRole,
                    IpAddress = currentUser?.IpAddress,
                    UserAgent = currentUser?.UserAgent,
                    Action = entry.State == EntityState.Added ? "Insert" : entry.State == EntityState.Deleted ? "Delete" : "Update",
                    Entity = entityName,
                    EntityName = entityName,
                    EntityId = entity.Id == 0 ? null : entity.Id,
                    PerformedBy = currentUser?.UserId?.ToString() ?? "system",
                    Timestamp = now,
                    AuditPayload = JsonSerializer.Serialize(changes),
                    Details = JsonSerializer.Serialize(changes)
                });
            }

            return base.SaveChangesAsync(cancellationToken);
        }

    }
}
