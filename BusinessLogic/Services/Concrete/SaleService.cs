using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Onion.Domain.Finance;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Common.Exceptions;
using Onion.Domain;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class SaleService : ISaleService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SaleService> _logger;
        private readonly Onion.BussinesLogic.Services.Abstract.IPaginationService _paginationService;
        private readonly Onion.BussinesLogic.Services.Abstract.IFiscalSequenceService? _fiscalSequenceService;
        private readonly Onion.Common.Services.ICurrentUserService? _currentUserService;

        public SaleService(IUnitOfWork uow, ILogger<SaleService> logger, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService, Onion.BussinesLogic.Services.Abstract.IFiscalSequenceService fiscalSequenceService, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _uow = uow;
            _logger = logger;
            _paginationService = paginationService;
            _fiscalSequenceService = fiscalSequenceService;
            _currentUserService = currentUserService;
        }

        public SaleService(IUnitOfWork uow, ILogger<SaleService> logger, Onion.BussinesLogic.Services.Abstract.IPaginationService paginationService)
        {
            _uow = uow;
            _logger = logger;
            _paginationService = paginationService;
        }

        public async Task<IEnumerable<Sale>> GetAllAsync()
        {
            return await _uow.Sales.ListAsync();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Sale>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            return await _uow.Sales.GetPagedAsync(pn, ps);
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            return await _uow.Sales.GetByIdAsync(id);
        }

        public async Task<Sale> CreateAsync(Sale sale)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            if (_currentUserService is not null)
            {
                if (_currentUserService.CompanyId is not > 0)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "COMPANY_REQUIRED", Message = "A valid company is required.", Language = "ES" });
                if (sale.CompanyId <= 0) sale.CompanyId = _currentUserService.CompanyId.Value;
                if (sale.CompanyId != _currentUserService.CompanyId)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "The sale belongs to another company.", Language = "ES" });
            }
            if (sale.Details == null || sale.Details.Count == 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "NO_ITEMS", Message = "Sale must have at least one item", Language = "ES" });

            if (!string.IsNullOrWhiteSpace(sale.IdempotencyKey))
            {
                var normalizedKey = sale.IdempotencyKey.Trim();
                sale.IdempotencyKey = normalizedKey;
                var existing = (await _uow.Sales.FindAsync(s => s.IdempotencyKey == normalizedKey && !s.IsDeleted)).FirstOrDefault();
                if (existing != null)
                    return existing;
            }

            if (sale.CashRegisterId.HasValue)
            {
                var register = await _uow.CashRegisters.GetByIdAsync(sale.CashRegisterId.Value);
                if (register == null || register.Status != CashRegisterStatus.OPEN)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "CASH_REGISTER_NOT_OPEN", Message = "The cash register must be open to record sales", Language = "ES" });
            }

            // Basic business rules for a retail store (papelería):
            // - Total must be >= sum of details unit price * qty
            decimal calcTotal = 0m;
            foreach (var d in sale.Details)
            {
                if (d.Quantity <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_QTY", Message = "Quantity must be greater than zero", Language = "ES" });
                if (d.UnitPrice <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_PRICE", Message = "UnitPrice must be greater than zero", Language = "ES" });
                calcTotal += d.Quantity * d.UnitPrice;
            }

            if (sale.Total <= 0 || sale.Total < calcTotal)
                sale.Total = calcTotal;

            // Set status based on paid amount
            if (sale.PaidAmount >= sale.Total)
                sale.Status = SaleStatus.PAID;
            else if (sale.PaidAmount > 0)
                sale.Status = SaleStatus.PARTIAL;
            else
                sale.Status = SaleStatus.PENDING;

            // Use transaction to ensure sale, details and inventory updates are atomic
            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                await _uow.Sales.AddAsync(sale);
                await _uow.SaveChangesAsync();
                // Reserve stock for each product first to avoid race conditions
                var reserved = new List<(int productId, decimal qty, int warehouseId)>();
                try
                {
                    foreach (var d in sale.Details)
                    {
                        var product = await _uow.Products.GetByIdAsync(d.ProductId);
                        if (product != null && !product.InvoiceWithoutStock)
                        {
                            if (d.WarehouseId.HasValue && d.WarehouseId.Value > 0)
                            {
                                var whId = d.WarehouseId.Value;
                                var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(d.ProductId, whId);
                                if (inv == null || inv.Quantity < d.Quantity)
                                    throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = $"Insufficient stock for product {product.Description} in warehouse {whId}", Language = "ES" });

                                inv.Quantity -= d.Quantity;
                                _uow.Inventories.Update(inv);
                                reserved.Add((d.ProductId, d.Quantity, whId));
                            }
                            else
                            {
                                var okReserve = await _uow.Products.TryReserveStockAsync(d.ProductId, d.Quantity);
                                if (!okReserve)
                                    throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = $"Insufficient stock for product {product.Description}", Language = "ES" });

                                reserved.Add((d.ProductId, d.Quantity, 0));
                            }
                        }
                    }

                    // At this point reservations succeeded; finalize by reducing actual stock
                    foreach (var r in reserved)
                    {
                        if (r.warehouseId == 0)
                        {
                            var ok = await _uow.Products.TryCommitReservedStockAsync(r.productId, r.qty);
                            if (!ok)
                                throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = $"Insufficient stock when finalizing product {r.productId}", Language = "ES" });
                        }
                        try
                        {
                            // Determine performing user from sale.CreateBy if numeric
                            int performedBy = 0;
                            if (!string.IsNullOrWhiteSpace(sale.CreateBy) && int.TryParse(sale.CreateBy, out var parsed))
                                performedBy = parsed;

                            // Record inventory movement (audit)
                            var mv = new Onion.Domain.Inventory.InventoryMovement
                            {
                                CompanyId = sale.CompanyId,
                                Type = Onion.Domain.Inventory.MovementType.Sale,
                                ProductId = r.productId,
                                Quantity = r.qty,
                                WarehouseId = r.warehouseId,
                                PerformedByUserId = performedBy,
                                Reference = sale.Id.ToString(),
                                Comment = "Sale deduction."
                            };
                            await _uow.InventoryMovements.AddAsync(mv);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Failed to record inventory movement for product {ProductId} on sale {SaleId}", r.productId, sale.Id);
                            throw;
                        }
                    }
                }
                catch
                {
                    // Release any reservations
                    foreach (var r in reserved)
                    {
                        if (r.warehouseId == 0)
                        {
                            try { await _uow.Products.ReleaseReservedStockAsync(r.productId, r.qty); } catch { }
                        }
                        else
                        {
                            try
                            {
                                var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(r.productId, r.warehouseId);
                                if (inv != null)
                                {
                                    inv.Quantity += r.qty;
                                    _uow.Inventories.Update(inv);
                                }
                            }
                            catch { }
                        }
                    }
                    throw;
                }

                // Create cash movement for sale (if associated cash register exists)
                if (sale.CashRegisterId.HasValue && sale.PaidAmount > 0)
                {
                    var cashMovement = new CashMovement
                    {
                        CashRegisterId = sale.CashRegisterId.Value,
                        Amount = sale.PaidAmount,
                        Description = $"Sale #{sale.Id}",
                        CompanyId = sale.CompanyId,
                        CashSessionId = sale.CashSessionId,
                        Type = CashMovementType.Sale,
                        Reason = "Sale"
                    };

                    await _uow.CashMovements.AddAsync(cashMovement);
                }

                if (_fiscalSequenceService is null)
                    throw new InvalidOperationException("Fiscal sequence service is not configured.");
                var ncf = await _fiscalSequenceService.NextFiscalNumberAsync(sale.CompanyId, sale.VoucherType);
                sale.InvoiceFolio = ncf;
                var fiscalDocument = Onion.Domain.Invoices.FiscalDocument.ForSale(sale.CompanyId, sale.Id, ncf);
                await _uow.FiscalDocuments.AddAsync(fiscalDocument);

                await _uow.SaveChangesAsync();
                await tx.CommitAsync();
                return sale;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(Sale sale)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            var existing = await _uow.Sales.GetByIdAsync(sale.Id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });

            existing.CustomerId = sale.CustomerId;
            existing.Total = sale.Total;
            existing.PaidAmount = sale.PaidAmount;
            existing.Status = sale.Status;
            existing.DueDate = sale.DueDate;

            _uow.Sales.Update(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Sales.GetByIdAsync(id) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });
            _uow.Sales.Remove(existing);
            await _uow.SaveChangesAsync();
        }

        public async Task CancelAsync(int saleId, string reason)
        {
            var sale = await _uow.Sales.GetByIdAsync(saleId) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });
            if (sale.Status == SaleStatus.PAID || sale.Status == SaleStatus.PARTIAL || sale.Status == SaleStatus.PENDING)
            {
                // Reverse stock for each sale detail
                foreach (var d in sale.Details)
                {
                    var warehouseId = d.WarehouseId.HasValue && d.WarehouseId.Value > 0 ? d.WarehouseId.Value : 0;
                    if (warehouseId > 0)
                    {
                        var inv = await _uow.Inventories.GetByProductAndWarehouseAsync(d.ProductId, warehouseId);
                        if (inv != null)
                        {
                            inv.Quantity += d.Quantity;
                            _uow.Inventories.Update(inv);
                        }
                    }
                    else
                    {
                        // increase product-level stock back
                        await _uow.Products.TryIncreaseStockAsync(d.ProductId, d.Quantity);
                    }
                    // record inventory movement
                    try
                    {
                        var mv = new Onion.Domain.Inventory.InventoryMovement
                        {
                            CompanyId = sale.CompanyId,
                            Type = Onion.Domain.Inventory.MovementType.In,
                            ProductId = d.ProductId,
                            Quantity = d.Quantity,
                            WarehouseId = warehouseId,
                            PerformedByUserId = 0,
                            Reference = sale.Id.ToString(),
                            Comment = $"Sale cancelled: {reason}"
                        };
                        await _uow.InventoryMovements.AddAsync(mv);
                    }
                    catch { }
                }

                sale.Status = SaleStatus.CANCELLED;
                _uow.Sales.Update(sale);
                await _uow.SaveChangesAsync();
            }
            else
            {
                throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_OPERATION", Message = "Sale cannot be cancelled", Language = "ES" });
            }
        }

        public async Task AddPaymentAsync(int saleId, Payment payment, string? method = null, int? cashRegisterId = null)
        {
            var sale = await _uow.Sales.GetByIdAsync(saleId);
            if (sale == null)
            {
                // Try to detect if sale exists but is filtered by tenant scope
                var existsElsewhere = await (_uow.Sales as DataAccess.Repositories.Abstract.ISaleRepository)?.GetByIdIgnoreQueryFiltersAsync(saleId);
                if (existsElsewhere != null)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "FORBIDDEN", Message = "Sale exists but access is forbidden for current tenant", Language = "ES" });

                throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });
            }
            if (payment.Amount <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_AMOUNT", Message = "Payment amount must be greater than zero", Language = "ES" });

            // attach payment to sale
            payment.SaleId = saleId;
            // Map method string to enum PaymentMethod
            if (!string.IsNullOrWhiteSpace(method) && System.Enum.TryParse<PaymentMethod>(method, true, out var pm))
                payment.PaymentMethod = pm;
            else
                payment.PaymentMethod = PaymentMethod.OTHER;
            await _uow.Payments.AddAsync(payment);
            sale.PaidAmount += payment.Amount;

            if (sale.PaidAmount >= sale.Total) sale.Status = SaleStatus.PAID;
            else if (sale.PaidAmount > 0) sale.Status = SaleStatus.PARTIAL;

            _uow.Sales.Update(sale);
            await _uow.SaveChangesAsync();

            // Create cash movement when cashRegisterId provided
            if (cashRegisterId.HasValue && payment.Amount > 0)
            {
                try
                {
                    var cm = new CashMovement
                    {
                        CashRegisterId = cashRegisterId.Value,
                        Amount = payment.Amount,
                        Description = $"Payment for sale {saleId}",
                        CompanyId = sale.CompanyId,
                        CreateBy = payment.CreateBy
                    };
                    await _uow.CashMovements.AddAsync(cm);
                    await _uow.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to record cash movement for sale payment {SaleId}", saleId);
                }
            }
        }
    }
}
