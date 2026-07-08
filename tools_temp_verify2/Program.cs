using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Onion.DataAccess;
using Onion.Domain.Invitations;
using Onion.Domain.Warehouses;

class StaticTenantProvider : ITenantProvider
{
    private readonly int? _companyId;
    public StaticTenantProvider(int? companyId) => _companyId = companyId;
    public int? GetCompanyId() => _companyId;
}

class Program
{
    static int Main(string[] args)
    {
        var options = new DbContextOptionsBuilder<OnionDbContext>()
            .UseInMemoryDatabase("verify-tenant-filter-db-temp2")
            .Options;

        // Seed database with two companies' data for four entity types
        using (var ctx = new OnionDbContext(options, new StaticTenantProvider(null)))
        {
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();

            // Invitations
            ctx.Invitations.Add(new Invitation { Email = "a@c.com", CompanyId = 1, Token = "t1", ExpiresAt = DateTime.UtcNow.AddDays(1) });
            ctx.Invitations.Add(new Invitation { Email = "b@c.com", CompanyId = 2, Token = "t2", ExpiresAt = DateTime.UtcNow.AddDays(1) });

            // Warehouses
            ctx.Warehouses.Add(new Warehouse { Name = "W1", CompanyId = 1 });
            ctx.Warehouses.Add(new Warehouse { Name = "W2", CompanyId = 2 });

            // Inventories
            ctx.Inventories.Add(new Inventory { ProductId = 101, WarehouseId = 1, Quantity = 10, CompanyId = 1 });
            ctx.Inventories.Add(new Inventory { ProductId = 101, WarehouseId = 2, Quantity = 20, CompanyId = 2 });

            // Movements
            ctx.Movements.Add(new Movement { ProductId = 101, FromWarehouseId = null, ToWarehouseId = 1, Quantity = 5, Type = MovementType.Inbound, CompanyId = 1, CreatedBy = "sys" });
            ctx.Movements.Add(new Movement { ProductId = 101, FromWarehouseId = null, ToWarehouseId = 2, Quantity = 7, Type = MovementType.Inbound, CompanyId = 2, CreatedBy = "sys" });

            ctx.SaveChanges();
        }

        // Helper to run a query and print results
        int RunQuery<T>(DbContextOptions<OnionDbContext> opts, ITenantProvider tenant, string name, Func<OnionDbContext, IQueryable<T>> q)
        {
            using (var ctx = new OnionDbContext(opts, tenant))
            {
                var list = q(ctx).ToList();
                Console.WriteLine($"{name} visible (tenant={tenant.GetCompanyId()?.ToString() ?? "null"}): {list.Count}");
                foreach (var item in list)
                {
                    switch (item)
                    {
                        case Invitation inv:
                            Console.WriteLine($" - Invitation {inv.Email} (CompanyId={inv.CompanyId})");
                            break;
                        case Warehouse w:
                            Console.WriteLine($" - Warehouse {w.Name} (CompanyId={w.CompanyId})");
                            break;
                        case Inventory it:
                            Console.WriteLine($" - Inventory P{it.ProductId} W{it.WarehouseId} (CompanyId={it.CompanyId})");
                            break;
                        case Movement mv:
                            Console.WriteLine($" - Movement P{mv.ProductId} Q{mv.Quantity} (CompanyId={mv.CompanyId})");
                            break;
                        default:
                            Console.WriteLine($" - {item}");
                            break;
                    }
                }
                return list.Count;
            }
        }

        // Check with tenant = 1
        var tenant1 = new StaticTenantProvider(1);
        RunQuery(options, tenant1, "Invitations", ctx => ctx.Invitations);
        RunQuery(options, tenant1, "Warehouses", ctx => ctx.Warehouses);
        RunQuery(options, tenant1, "Inventories", ctx => ctx.Inventories);
        RunQuery(options, tenant1, "Movements", ctx => ctx.Movements);

        // Check with no tenant
        var tenantNull = new StaticTenantProvider(null);
        RunQuery(options, tenantNull, "Invitations", ctx => ctx.Invitations);
        RunQuery(options, tenantNull, "Warehouses", ctx => ctx.Warehouses);
        RunQuery(options, tenantNull, "Inventories", ctx => ctx.Inventories);
        RunQuery(options, tenantNull, "Movements", ctx => ctx.Movements);

        return 0;
    }
}
