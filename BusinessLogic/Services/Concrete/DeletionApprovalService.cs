using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Domain.Authorization;
using Onion.Domain.Products;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class DeletionApprovalService : IDeletionApprovalService
{
    private readonly OnionDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private static readonly IReadOnlyDictionary<string, Func<OnionDbContext, IQueryable<BaseEntity>>> EntitySets =
        new Dictionary<string, Func<OnionDbContext, IQueryable<BaseEntity>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Product"] = db => db.Products,
            ["Category"] = db => db.Categories,
            ["Customer"] = db => db.Customers,
            ["Supplier"] = db => db.Suppliers,
            ["Sale"] = db => db.Sales,
            ["Purchase"] = db => db.Purchases,
            ["Return"] = db => db.Returns,
            ["Payment"] = db => db.Payments,
            ["CashRegister"] = db => db.CashRegisters,
            ["CashMovement"] = db => db.CashMovements,
            ["ProductType"] = db => db.ProductTypes,
            ["InventoryMovement"] = db => db.InventoryMovements,
            ["PurchaseOrderReceipt"] = db => db.PurchaseOrderReceipts,
            ["ManageRequest"] = db => db.ManageRequests,
            ["InvoiceSequence"] = db => db.InvoiceSequences,
            ["AccountReceivable"] = db => db.AccountReceivables,
            ["PaymentPlan"] = db => db.PaymentPlans,
            ["Installment"] = db => db.Installments,
            ["Credit"] = db => db.Credits,
            ["CreditNote"] = db => db.CreditNotes,
            ["Appointment"] = db => db.Appointments
        };

    public DeletionApprovalService(OnionDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private int CompanyId => _currentUser.CompanyId ?? throw new InvalidOperationException("Tenant company id is required.");

    public async Task<DeletionApprovalDto> RequestAsync(string entityType, int entityId, string? reason)
    {
        if (!EntitySets.ContainsKey(entityType)) throw new ArgumentException("Entity type is not eligible for approval.");
        if (!await EntitySets[entityType](_db).AnyAsync(e => e.Id == entityId)) throw new KeyNotFoundException("Entity not found.");
        var request = new DeletionApprovalRequest { CompanyId = CompanyId, EntityType = entityType, EntityId = entityId, RequestedByUserId = _currentUser.UserId ?? 0, Reason = reason, Status = "Pending" };
        _db.DeletionApprovalRequests.Add(request);
        await _db.SaveChangesAsync();
        return ToDto(request);
    }

    public async Task<IReadOnlyCollection<DeletionApprovalDto>> GetPendingAsync() => await _db.DeletionApprovalRequests.AsNoTracking()
        .Where(x => x.CompanyId == CompanyId && x.Status == "Pending")
        .OrderBy(x => x.RequestedAt).Select(x => new DeletionApprovalDto(x.Id, x.CompanyId, x.EntityType, x.EntityId, x.RequestedByUserId, x.RequestedAt, x.Reason, x.Status)).ToListAsync();

    public async Task ApproveAsync(int id, string? notes)
    {
        var request = await _db.DeletionApprovalRequests.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId && x.Status == "Pending")
            ?? throw new KeyNotFoundException("Pending approval not found.");
        if (!EntitySets.TryGetValue(request.EntityType, out var set)) throw new InvalidOperationException("Entity type is not eligible for approval.");
        var entity = await set(_db).SingleOrDefaultAsync(e => e.Id == request.EntityId) ?? throw new KeyNotFoundException("Entity no longer exists.");
        entity.IsDeleted = true;
        entity.Active = false;
        request.Status = "Approved";
        request.ReviewedByUserId = _currentUser.UserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = notes;
        await _db.SaveChangesAsync();
    }

    public async Task RejectAsync(int id, string? notes)
    {
        var request = await _db.DeletionApprovalRequests.SingleOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId && x.Status == "Pending")
            ?? throw new KeyNotFoundException("Pending approval not found.");
        request.Status = "Rejected";
        request.ReviewedByUserId = _currentUser.UserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewNotes = notes;
        await _db.SaveChangesAsync();
    }

    private static DeletionApprovalDto ToDto(DeletionApprovalRequest x) => new(x.Id, x.CompanyId, x.EntityType, x.EntityId, x.RequestedByUserId, x.RequestedAt, x.Reason, x.Status);
}
