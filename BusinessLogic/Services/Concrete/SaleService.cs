using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        public SaleService(IUnitOfWork uow, ILogger<SaleService> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<IEnumerable<Sale>> GetAllAsync()
        {
            return await _uow.Sales.ListAsync();
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            return await _uow.Sales.GetByIdAsync(id);
        }

        public async Task<Sale> CreateAsync(Sale sale)
        {
            if (sale == null) throw new ArgumentNullException(nameof(sale));
            if (sale.Details == null || sale.Details.Count == 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "NO_ITEMS", Message = "Sale must have at least one item", Language = "ES" });

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
                // Reserve stock for each product first to avoid race conditions
                var reserved = new List<(int productId, decimal qty)>();
                try
                {
                    foreach (var d in sale.Details)
                    {
                        var product = await _uow.Products.GetByIdAsync(d.ProductId);
                        if (product != null && !product.InvoiceWithoutStock)
                        {
                            var okReserve = await _uow.Products.TryReserveStockAsync(d.ProductId, d.Quantity);
                            if (!okReserve)
                                throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = $"Insufficient stock for product {product.Description}", Language = "ES" });

                            reserved.Add((d.ProductId, d.Quantity));
                        }
                    }

                    // At this point reservations succeeded; finalize by reducing actual stock
                    foreach (var r in reserved)
                    {
                        var ok = await _uow.Products.TryReduceStockAsync(r.productId, r.qty);
                        if (!ok)
                            throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_STOCK", Message = $"Insufficient stock when finalizing product {r.productId}", Language = "ES" });
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
                            WarehouseId = 0,
                            PerformedByUserId = performedBy,
                            Reference = sale.Id.ToString(),
                            Comment = "Sale deduction."
                        };
                        await _uow.InventoryMovements.AddAsync(mv);
                    }
                    catch
                    {
                        _logger?.LogWarning("Failed to record inventory movement for product {ProductId} on sale {SaleId}", r.productId, sale.Id);
                    }
                    }
                }
                catch
                {
                    // Release any reservations
                    foreach (var r in reserved)
                    {
                        try { await _uow.Products.ReleaseReservedStockAsync(r.productId, r.qty); } catch { }
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
                        CompanyId = sale.CompanyId
                    };

                    await _uow.CashMovements.AddAsync(cashMovement);
                }

                // Assign invoice folio (simple sequence per company)
                try
                {
                    var seqList = await _uow.InvoiceSequences.FindAsync(x => x.CompanyId == sale.CompanyId);
                    var seq = seqList.FirstOrDefault();
                    if (seq == null)
                    {
                        seq = new Onion.Domain.Invoices.InvoiceSequence { CompanyId = sale.CompanyId, LastFolio = 1 };
                        await _uow.InvoiceSequences.AddAsync(seq);
                        sale.InvoiceFolio = seq.LastFolio.ToString();
                    }
                    else
                    {
                        seq.LastFolio += 1;
                        _uow.InvoiceSequences.Update(seq);
                        sale.InvoiceFolio = seq.LastFolio.ToString();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to assign invoice folio for sale");
                }

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
                    // increase stock back
                    await _uow.Products.TryIncreaseStockAsync(d.ProductId, d.Quantity);
                    // record inventory movement
                    try
                    {
                        var mv = new Onion.Domain.Inventory.InventoryMovement
                        {
                            CompanyId = sale.CompanyId,
                            Type = Onion.Domain.Inventory.MovementType.In,
                            ProductId = d.ProductId,
                            Quantity = d.Quantity,
                            WarehouseId = 0,
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

        public async Task AddPaymentAsync(int saleId, Payment payment)
        {
            var sale = await _uow.Sales.GetByIdAsync(saleId) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });
            if (payment.Amount <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_AMOUNT", Message = "Payment amount must be greater than zero", Language = "ES" });

            // attach payment to sale
            payment.SaleId = saleId;
            await _uow.Payments.AddAsync(payment);
            sale.PaidAmount += payment.Amount;

            if (sale.PaidAmount >= sale.Total) sale.Status = SaleStatus.PAID;
            else if (sale.PaidAmount > 0) sale.Status = SaleStatus.PARTIAL;

            _uow.Sales.Update(sale);
            await _uow.SaveChangesAsync();
        }
    }
}
