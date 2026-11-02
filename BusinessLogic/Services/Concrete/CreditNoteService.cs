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
        private readonly Onion.BussinesLogic.Services.Abstract.ICashMovementService _cashMovementService;
        private readonly Onion.BussinesLogic.Services.Abstract.ICashRegisterService _cashRegisterService;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public CreditNoteService(
            IRepository<CreditNote> creditNoteRepo,
            OnionDbContext db,
            Onion.BussinesLogic.Services.Abstract.ICashMovementService cashMovementService,
            Onion.BussinesLogic.Services.Abstract.ICashRegisterService cashRegisterService,
            Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _creditNoteRepo = creditNoteRepo;
            _db = db;
            _cashMovementService = cashMovementService;
            _cashRegisterService = cashRegisterService;
            _currentUserService = currentUserService;
        }

        public async Task<CreditNoteDto> CreateAsync(int companyId, CreateCreditNoteRequest request)
        {
            // Use DB transaction to ensure atomicity of note, stock reintegration and cash movement
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Create credit note entity
                if (request.Details == null || request.Details.Count == 0)
                    throw new InvalidOperationException("La nota de crédito debe tener al menos un detalle.");

                if (request.Details.Any(d => d.Quantity <= 0))
                    throw new InvalidOperationException("La cantidad devuelta debe ser mayor que cero.");

                var sale = request.SaleId.HasValue
                    ? await _db.Sales.Include(s => s.Details)
                        .FirstOrDefaultAsync(s => s.Id == request.SaleId.Value && s.CompanyId == companyId)
                    : null;

                if (request.SaleId.HasValue && sale == null)
                    throw new InvalidOperationException("La venta asociada no existe para esta compañía.");

                var stockReturns = BuildStockReturns(request, sale);
                var note = new CreditNote
                {
                    CompanyId = companyId,
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

                // Return each product to the warehouse from which it was sold.
                foreach (var stockReturn in stockReturns)
                {
                    var performedBy = _currentUserService?.UserId?.ToString() ?? "system";
                    await AddStockInTransactionAsync(stockReturn.ProductId, stockReturn.WarehouseId, stockReturn.Quantity, companyId, performedBy, note.Number);
                }

                // Cash egress: if amount > 0, register a cash movement (egress)
                if (note.TotalAmount > 0)
                {
                    var cashRegisters = await _cashRegisterService.GetAllAsync();
                    var openRegister = cashRegisters.FirstOrDefault(r => r.CompanyId == companyId && r.IsOpen);
                    if (openRegister != null)
                    {
                        var movement = new Onion.Domain.CashMovement
                        {
                            CashRegisterId = openRegister.Id,
                            Description = $"Credit note refund {note.Number}",
                            Amount = -note.TotalAmount,
                            CompanyId = companyId
                        };
                        await _cashMovementService.AddAsync(movement);
                    }
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
                        throw new InvalidOperationException($"La venta no tiene almacén definido para el producto {detail.ProductId}.");

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

            var inventory = await _db.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);
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
