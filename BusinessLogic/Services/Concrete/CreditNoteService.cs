using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly IRepository<CreditNote> _creditNoteRepo;
        private readonly OnionDbContext _db;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;
        private readonly Onion.DataAccess.Tenant.ITenantMembershipValidator _membershipValidator;

        public CreditNoteService(
            IRepository<CreditNote> creditNoteRepo,
            OnionDbContext db,
            Onion.Common.Services.ICurrentUserService currentUserService,
            Onion.DataAccess.Tenant.ITenantMembershipValidator membershipValidator)
        {
            _creditNoteRepo = creditNoteRepo;
            _db = db;
            _currentUserService = currentUserService;
            _membershipValidator = membershipValidator;
        }

        public async Task<CreditNoteDto> CreateAsync(int companyId, CreateCreditNoteRequest request)
        {
            if (_currentUserService.UserId is not > 0 ||
                !_currentUserService.IsAuthenticated ||
                !await _membershipValidator.HasActiveAccessAsync(_currentUserService.UserId.Value, companyId))
                throw new UnauthorizedAccessException("El usuario no tiene una membresía activa en la compañía solicitada.");

            // Use DB transaction to ensure atomicity of note, stock reintegration and cash movement
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var activeDetails = request.GetResolvedDetails();

                if (activeDetails == null || activeDetails.Count == 0)
                    throw new InvalidOperationException("La nota de crédito debe tener al menos un detalle.");

                request.Details = activeDetails;

                var targetSaleId = request.SaleId ?? request.BillingId;
                if (targetSaleId is not > 0)
                    throw new InvalidOperationException("La factura original es requerida para procesar la devolución.");

                if (request.Details.Any(d => d.Quantity <= 0))
                    throw new InvalidOperationException("La cantidad devuelta debe ser mayor que cero.");

                var sale = await _db.Sales.Include(s => s.Details)
                    .FirstOrDefaultAsync(s => s.Id == targetSaleId.Value && s.CompanyId == companyId);

                if (sale == null)
                    throw new InvalidOperationException("La venta asociada no existe para esta compañía.");

                // Validar que cada producto pertenezca a la venta original si se especificó factura
                if (sale != null)
                {
                    var returnedByProduct = await _db.CreditNotes
                        .Where(x => x.CompanyId == companyId && x.SaleId == sale.Id && !x.IsDeleted)
                        .SelectMany(x => x.Details)
                        .GroupBy(x => x.ProductId)
                        .ToDictionaryAsync(x => x.Key, x => x.Sum(d => d.Quantity));

                    foreach (var d in request.Details.GroupBy(x => x.ProductId).Select(x => new { ProductId = x.Key, Quantity = x.Sum(y => y.Quantity) }))
                    {
                        var saleProduct = sale.Details.FirstOrDefault(sd => sd.ProductId == d.ProductId);
                        if (saleProduct == null)
                            throw new InvalidOperationException($"El producto {d.ProductId} no pertenece a la venta original.");

                        var totalSold = sale.Details.Where(sd => sd.ProductId == d.ProductId).Sum(sd => sd.Quantity);
                        var alreadyReturned = returnedByProduct.GetValueOrDefault(d.ProductId);
                        if (d.Quantity + alreadyReturned > totalSold)
                            throw new InvalidOperationException($"La cantidad devuelta ({d.Quantity + alreadyReturned}) del producto {d.ProductId} supera la cantidad vendida original ({totalSold}).");
                    }
                }

                // Generar reingresos de almacén solo si no es ajuste de precio (CreditNoteType != 3)
                var isPriceAdjustment = request.CreditNoteType.HasValue && request.CreditNoteType.Value == 3;
                var stockReturns = !isPriceAdjustment ? BuildStockReturns(request, sale) : new List<StockReturn>();

                var note = new CreditNote
                {
                    CompanyId = companyId,
                    SaleId = sale.Id,
                    CustomerId = request.CustomerId,
                    Date = DateTime.UtcNow,
                    Reason = request.Reason
                };

                // generate number simple sequential (could be improved)
                note.Number = $"CN-{DateTime.UtcNow:yyyyMMddHHmmss}";

                foreach (var d in request.Details)
                {
                    var nd = new CreditNoteDetail
                    {
                        ProductId = d.ProductId,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice
                    };
                    note.Details.Add(nd);
                }

                note.TotalAmount = note.Details.Sum(x => x.LineTotal);

                await _creditNoteRepo.AddAsync(note);
                await _db.SaveChangesAsync();

                // Reingresar stock a almacén si aplica
                if (!isPriceAdjustment)
                {
                    foreach (var stockReturn in stockReturns)
                    {
                        var performedBy = _currentUserService?.UserId?.ToString() ?? "system";
                        await AddStockInTransactionAsync(stockReturn.ProductId, stockReturn.WarehouseId, stockReturn.Quantity, companyId, performedBy, note.Number);
                    }
                }

                // Movimiento de caja: aplicar egreso si el método de reembolso es en efectivo (CASH)
                var isCashRefund = string.Equals(request.RefundMethod, "CASH", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(request.RefundMethod, "EFECTIVO", StringComparison.OrdinalIgnoreCase);

                if (isCashRefund && note.TotalAmount > 0)
                {
                    var openRegister = await _db.CashRegisters
                        .FirstOrDefaultAsync(r => r.CompanyId == companyId && r.IsOpen && r.Active && !r.IsDeleted);
                    if (openRegister == null)
                        throw new InvalidOperationException("Se requiere una caja abierta para reembolsar en efectivo.");

                    var openSession = await _db.CashSessions
                        .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.CashRegisterId == openRegister.Id &&
                                                  s.Status == Onion.Domain.Finance.CashSessionStatus.Open);
                    if (openSession == null)
                        throw new InvalidOperationException("Se requiere una sesión de caja abierta para reembolsar en efectivo.");

                    openSession.ApplyMovement(Onion.Domain.Finance.CashMovementType.CashOut, note.TotalAmount);
                    await _db.CashMovements.AddAsync(new Onion.Domain.CashMovement
                    {
                        CashRegisterId = openRegister.Id,
                        CashSessionId = openSession.Id,
                        Description = $"Reembolso nota de crédito {note.Number}",
                        Reason = $"Reembolso nota de crédito {note.Number}",
                        Amount = note.TotalAmount,
                        Type = Onion.Domain.Finance.CashMovementType.CashOut,
                        CompanyId = companyId,
                        ApprovedByUserId = _currentUserService.UserId
                    });
                    await _db.SaveChangesAsync();
                }

                await tx.CommitAsync();

                var dto = new CreditNoteDto
                {
                    Id = note.Id,
                    Number = note.Number,
                    Date = note.Date,
                    CompanyId = note.CompanyId,
                    CustomerId = note.CustomerId,
                    TotalAmount = note.TotalAmount,
                    Reason = note.Reason,
                    Details = note.Details.Select(x => new CreditNoteDetailDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        Quantity = x.Quantity,
                        UnitPrice = x.UnitPrice,
                        LineTotal = x.LineTotal
                    }).ToList()
                };

                return dto;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private List<StockReturn> BuildStockReturns(CreateCreditNoteRequest request, Sale? sale)
        {
            var result = new List<StockReturn>();

            foreach (var detail in request.Details.GroupBy(d => d.ProductId).Select(g => new CreateCreditNoteDetail
            {
                ProductId = g.Key,
                Quantity = g.Sum(d => d.Quantity),
                UnitPrice = g.First().UnitPrice,
                WarehouseId = g.First().WarehouseId
            }))
            {
                var remaining = detail.Quantity;
                var saleDetails = sale?.Details
                    .Where(d => d.ProductId == detail.ProductId && d.WarehouseId.HasValue && d.WarehouseId.Value > 0)
                    .ToList();

                if (sale != null)
                {
                    if (saleDetails == null || saleDetails.Count == 0)
                    {
                        var fallbackWarehouseId = detail.WarehouseId ?? request.WarehouseId;
                        if (!fallbackWarehouseId.HasValue || fallbackWarehouseId.Value <= 0)
                        {
                            var defaultWarehouse = _db.Warehouses.FirstOrDefault(w => w.CompanyId == sale.CompanyId && w.Active && !w.IsDeleted);
                            fallbackWarehouseId = defaultWarehouse?.Id;
                        }
                        if (fallbackWarehouseId.HasValue && fallbackWarehouseId.Value > 0)
                        {
                            result.Add(new StockReturn(detail.ProductId, fallbackWarehouseId.Value, detail.Quantity));
                            continue;
                        }
                        throw new InvalidOperationException($"La venta no tiene almacén definido para el producto {detail.ProductId}.");
                    }

                    foreach (var saleDetail in saleDetails)
                    {
                        if (remaining <= 0) break;
                        var quantity = Math.Min(remaining, saleDetail.Quantity);
                        result.Add(new StockReturn(detail.ProductId, saleDetail.WarehouseId!.Value, quantity));
                        remaining -= quantity;
                    }

                    if (remaining > 0)
                        throw new InvalidOperationException($"La cantidad devuelta del producto {detail.ProductId} supera la cantidad vendida.");
                }
                else
                {
                    if (!detail.WarehouseId.HasValue || detail.WarehouseId.Value <= 0)
                        throw new InvalidOperationException($"Debe indicar el almacén de devolución para el producto {detail.ProductId}.");

                    result.Add(new StockReturn(detail.ProductId, detail.WarehouseId.Value, detail.Quantity));
                }
            }

            return result
                .GroupBy(x => new { x.ProductId, x.WarehouseId })
                .Select(g => new StockReturn(g.Key.ProductId, g.Key.WarehouseId, g.Sum(x => x.Quantity)))
                .ToList();
        }

        private async Task AddStockInTransactionAsync(int productId, int warehouseId, decimal quantity, int companyId, string performedBy, string reference)
        {
            var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId && w.CompanyId == companyId && w.Active && !w.IsDeleted);
            if (warehouse == null)
                throw new InvalidOperationException($"El almacén {warehouseId} no pertenece a la compañía o está inactivo.");

            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId && i.CompanyId == companyId);
            if (inventory == null)
            {
                inventory = new Onion.Domain.Warehouses.Inventory
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    CompanyId = companyId,
                    Quantity = quantity,
                    CreateBy = performedBy,
                    ModifiedBy = performedBy
                };
                await _db.Inventories.AddAsync(inventory);
            }
            else
            {
                inventory.Quantity += quantity;
                inventory.ModifiedBy = performedBy;
            }

            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId && p.CompanyId == companyId);
            if (product == null)
                throw new InvalidOperationException($"El producto {productId} no pertenece a la compañía.");

            product.Stock += quantity;
            product.ModifiedBy = performedBy;

            await _db.Movements.AddAsync(new Onion.Domain.Warehouses.Movement
            {
                ProductId = productId,
                ToWarehouseId = warehouseId,
                Quantity = quantity,
                Type = Onion.Domain.Warehouses.MovementType.Inbound,
                CompanyId = companyId,
                CreatedBy = performedBy,
                CreateBy = performedBy,
                ModifiedBy = performedBy
            });
            await _db.SaveChangesAsync();
        }

        private sealed record StockReturn(int ProductId, int WarehouseId, decimal Quantity);
    }
}
