using System;
using System.Collections.Generic;
using System.Linq;
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
            var repo = _uow.Sales as DataAccess.Repositories.Abstract.ISaleRepository;
            if (repo != null)
            {
                return await repo.ListWithDetailsAsync();
            }
            return await _uow.Sales.ListAsync();
        }

        public async Task<IEnumerable<Sale>> GetFilteredAsync(SaleOriginChannel? originChannel, DateTime? startDate, DateTime? endDate, int? customerId)
        {
            var repo = _uow.Sales as DataAccess.Repositories.Abstract.ISaleRepository;
            var sales = repo != null ? await repo.ListWithDetailsAsync() : (await _uow.Sales.ListAsync()).ToList();
            var query = sales.AsEnumerable();
            if (originChannel.HasValue) query = query.Where(x => x.OriginChannel == originChannel.Value);
            if (startDate.HasValue) query = query.Where(x => x.Date >= startDate.Value);
            if (endDate.HasValue) query = query.Where(x => x.Date < endDate.Value.Date.AddDays(1));
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
            return query.OrderByDescending(x => x.Date).ToList();
        }

        public async Task<Onion.Common.Models.Pagination.PagedList<Sale>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);
            var repo = _uow.Sales as DataAccess.Repositories.Abstract.ISaleRepository;
            if (repo != null)
            {
                return await repo.GetPagedWithDetailsAsync(pn, ps);
            }
            return await _uow.Sales.GetPagedAsync(pn, ps);
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            var repo = _uow.Sales as DataAccess.Repositories.Abstract.ISaleRepository;
            if (repo != null)
            {
                return await repo.GetByIdWithDetailsAsync(id);
            }
            return await _uow.Sales.GetByIdAsync(id);
        }

        public async Task<Sale> CreateAsync(Sale sale, IReadOnlyCollection<Payment>? payments = null)
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

            var paymentLines = payments?.Where(p => p is not null).ToList() ?? new List<Payment>();
            if (paymentLines.Count == 0 && sale.PaidAmount > 0)
            {
                paymentLines.Add(new Payment
                {
                    Amount = sale.PaidAmount,
                    PaymentMethod = sale.PaymentType == PaymentType.CREDIT ? PaymentMethod.OTHER : PaymentMethod.CASH
                });
            }

            if (sale.OriginChannel == SaleOriginChannel.CREDITO)
                sale.PaymentType = PaymentType.CREDIT;

            if (sale.VoucherType == 0)
                sale.VoucherType = Onion.Domain.Invoices.VoucherType.B02;

            if (sale.VoucherType == Onion.Domain.Invoices.VoucherType.B01 && string.IsNullOrWhiteSpace(sale.FiscalRnc))
                throw new CustomException(new Onion.Common.Models.Error { Code = "FISCAL_RNC_REQUIRED", Message = "Fiscal RNC is required for B01 invoices.", Language = "ES" });

            // Auto-resolve or validate CashSession and CashRegister
            if (sale.CashRegisterId.HasValue && !sale.CashSessionId.HasValue)
            {
                var activeSession = (await _uow.CashSessions.FindAsync(s => s.CashRegisterId == sale.CashRegisterId.Value && s.Status == CashSessionStatus.Open && s.CompanyId == sale.CompanyId)).FirstOrDefault();
                if (activeSession != null)
                {
                    sale.CashSessionId = activeSession.Id;
                }
            }

            if (sale.CashSessionId.HasValue)
            {
                var session = await _uow.CashSessions.GetByIdAsync(sale.CashSessionId.Value);

                // If session is null, closed, or belongs to another register, attempt auto-recovery
                if (session is null || session.Status != CashSessionStatus.Open || (sale.CashRegisterId.HasValue && session.CashRegisterId != sale.CashRegisterId.Value))
                {
                    Onion.Domain.Finance.CashSession? fallbackSession = null;
                    if (sale.CashRegisterId.HasValue)
                    {
                        fallbackSession = (await _uow.CashSessions.FindAsync(s => s.CashRegisterId == sale.CashRegisterId.Value && s.Status == CashSessionStatus.Open && s.CompanyId == sale.CompanyId)).FirstOrDefault();
                    }
                    if (fallbackSession == null)
                    {
                        fallbackSession = (await _uow.CashSessions.FindAsync(s => s.Status == CashSessionStatus.Open && s.CompanyId == sale.CompanyId)).FirstOrDefault();
                    }

                    if (fallbackSession != null)
                    {
                        session = fallbackSession;
                        sale.CashSessionId = session.Id;
                        sale.CashRegisterId = session.CashRegisterId;
                    }
                }

                // If session is still null but register is open, auto-create the missing session
                if (session is null && sale.CashRegisterId.HasValue)
                {
                    var reg = await _uow.CashRegisters.GetByIdAsync(sale.CashRegisterId.Value);
                    if (reg != null && reg.Status == CashRegisterStatus.OPEN)
                    {
                        var newSession = new Onion.Domain.Finance.CashSession
                        {
                            CompanyId = sale.CompanyId,
                            CashRegisterId = reg.Id,
                            CashierUserId = reg.OpenedByUserId ?? 1,
                            OpeningAmount = reg.OpeningAmount,
                            OpenedAt = reg.OpenedAt
                        };
                        await _uow.CashSessions.AddAsync(newSession);
                        await _uow.SaveChangesAsync();
                        session = newSession;
                        sale.CashSessionId = newSession.Id;
                    }
                }

                if (session is null || session.Status != CashSessionStatus.Open ||
                    (sale.CashRegisterId.HasValue && session.CashRegisterId != sale.CashRegisterId.Value))
                    throw new CustomException(new Onion.Common.Models.Error { Code = "CASH_SESSION_NOT_OPEN", Message = "The selected cash session is not open or does not belong to the selected cash register.", Language = "EN" });
                sale.CashRegisterId ??= session.CashRegisterId;
            }
            else if (sale.OriginChannel == SaleOriginChannel.POS_PRESENCIAL)
            {
                Onion.Domain.Finance.CashSession? activeSession = null;
                if (sale.CashRegisterId.HasValue)
                {
                    activeSession = (await _uow.CashSessions.FindAsync(s => s.CashRegisterId == sale.CashRegisterId.Value && s.Status == CashSessionStatus.Open && s.CompanyId == sale.CompanyId)).FirstOrDefault();
                }
                if (activeSession == null)
                {
                    activeSession = (await _uow.CashSessions.FindAsync(s => s.Status == CashSessionStatus.Open && s.CompanyId == sale.CompanyId)).FirstOrDefault();
                }

                if (activeSession != null)
                {
                    sale.CashSessionId = activeSession.Id;
                    sale.CashRegisterId ??= activeSession.CashRegisterId;
                }
                else if (sale.CashRegisterId.HasValue)
                {
                    var reg = await _uow.CashRegisters.GetByIdAsync(sale.CashRegisterId.Value);
                    if (reg != null && reg.Status == CashRegisterStatus.OPEN)
                    {
                        var newSession = new Onion.Domain.Finance.CashSession
                        {
                            CompanyId = sale.CompanyId,
                            CashRegisterId = reg.Id,
                            CashierUserId = reg.OpenedByUserId ?? 1,
                            OpeningAmount = reg.OpeningAmount,
                            OpenedAt = reg.OpenedAt
                        };
                        await _uow.CashSessions.AddAsync(newSession);
                        await _uow.SaveChangesAsync();
                        sale.CashSessionId = newSession.Id;
                    }
                    else
                    {
                        throw new CustomException(new Onion.Common.Models.Error { Code = "CASH_SESSION_REQUIRED", Message = "A POS sale requires an open cash session.", Language = "EN" });
                    }
                }
                else
                {
                    throw new CustomException(new Onion.Common.Models.Error { Code = "CASH_SESSION_REQUIRED", Message = "A POS sale requires an open cash session.", Language = "EN" });
                }
            }

            if (sale.Details == null || sale.Details.Count == 0)
                throw new CustomException(new Onion.Common.Models.Error { Code = "NO_ITEMS", Message = "Sale must have at least one item", Language = "ES" });

            var isPosSale = sale.OriginChannel == SaleOriginChannel.POS_PRESENCIAL
                         || sale.CashSessionId.HasValue
                         || sale.CashRegisterId.HasValue;

            if (sale.CashRegisterId.HasValue)
            {
                var register = await _uow.CashRegisters.GetByIdAsync(sale.CashRegisterId.Value);
                if (register == null || register.Status != CashRegisterStatus.OPEN)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "CASH_REGISTER_NOT_OPEN", Message = "The cash register must be open to record sales", Language = "ES" });
            }

            if (isPosSale)
            {
                foreach (var d in sale.Details)
                {
                    var p = await _uow.Products.GetByIdAsync(d.ProductId);
                    if (p != null)
                    {
                        var pType = p.ProductTypeId > 0 ? await _uow.ProductTypes.GetByIdAsync(p.ProductTypeId) : null;
                        var isServiceProduct = p.IsService
                            || p.ProductTypeId == 2
                            || string.Equals(pType?.Description?.Trim(), "Servicio", StringComparison.OrdinalIgnoreCase);

                        if (isServiceProduct)
                        {
                            throw new CustomException(new Onion.Common.Models.Error
                            {
                                Code = "SERVICE_NOT_ALLOWED_IN_POS",
                                Message = $"Los servicios no pueden cobrarse a través de la caja rápida ('{p.Description}'). Utilice el módulo de Facturación o Venta de Servicios.",
                                Language = "ES"
                            });
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(sale.IdempotencyKey))
            {
                var normalizedKey = sale.IdempotencyKey.Trim();
                sale.IdempotencyKey = normalizedKey;
                var existing = (await _uow.Sales.FindAsync(s => s.IdempotencyKey == normalizedKey && !s.IsDeleted)).FirstOrDefault();
                if (existing != null)
                    return existing;
            }

            // Basic business rules for a retail store (papelería):
            // - Total must be >= sum of details unit price * qty
            decimal grossTotal = 0m;
            decimal lineDiscountTotal = 0m;
            decimal lineTaxTotal = 0m;
            foreach (var d in sale.Details)
            {
                if (d.Quantity <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_QTY", Message = "Quantity must be greater than zero", Language = "ES" });
                if (d.UnitPrice <= 0) throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_PRICE", Message = "UnitPrice must be greater than zero", Language = "ES" });
                var lineGross = d.Quantity * d.UnitPrice;
                if (d.Discount < 0 || d.Discount > lineGross)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_DISCOUNT", Message = "Line discount must be between zero and the line total.", Language = "ES" });
                if (d.Tax < 0)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_TAX", Message = "Line tax cannot be negative.", Language = "ES" });
                grossTotal += lineGross;
                lineDiscountTotal += d.Discount;
                lineTaxTotal += d.Tax;
            }

            var calcTotal = grossTotal - lineDiscountTotal + lineTaxTotal;
            if (sale.Total <= 0 || sale.Total < calcTotal)
                sale.Total = calcTotal;
            sale.SubTotal = grossTotal;
            if (sale.Discount <= 0)
                sale.Discount = lineDiscountTotal;
            if (sale.Tax <= 0)
                sale.Tax = lineTaxTotal;

            var paymentTotal = 0m;
            foreach (var payment in paymentLines)
            {
                if (paymentLines.Count == 1 && payment.Amount <= 0)
                    payment.Amount = sale.Total;
                if (payment.Amount <= 0)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "INVALID_PAYMENT_AMOUNT", Message = "Payment amount must be greater than zero.", Language = "EN" });
                if (payment.PaymentMethod == PaymentMethod.CASH && payment.AmountReceived.HasValue && payment.AmountReceived.Value < payment.Amount)
                    throw new CustomException(new Onion.Common.Models.Error { Code = "INSUFFICIENT_AMOUNT_RECEIVED", Message = "Cash received cannot be less than the payment amount.", Language = "EN" });
                if (payment.PaymentMethod == PaymentMethod.CASH && payment.AmountReceived.HasValue && !payment.ChangeAmount.HasValue)
                    payment.ChangeAmount = payment.AmountReceived.Value - payment.Amount;
                paymentTotal += payment.Amount;
            }

            if (paymentLines.Count > 0 && paymentTotal > sale.Total && sale.PaymentType != PaymentType.CREDIT)
                throw new CustomException(new Onion.Common.Models.Error { Code = "PAYMENT_EXCEEDS_SALE", Message = "Payment total cannot exceed the sale total.", Language = "EN" });

            if (paymentLines.Count > 0)
                sale.PaidAmount = paymentTotal;

            // Set status based on paid amount
            if (sale.PaidAmount >= sale.Total)
                sale.Status = SaleStatus.PAID;
            else if (sale.PaidAmount > 0)
                sale.Status = SaleStatus.PARTIAL;
            else
                sale.Status = SaleStatus.PENDING;

            // Validate and deduct inventory before persisting the sale. Everything is committed atomically.
            using var tx = await _uow.BeginTransactionAsync();
            try
            {
                var companySettingsList = await _uow.CompanySettingsRepo.FindAsync(cs => cs.CompanyId == sale.CompanyId);
                var companySettings = companySettingsList.FirstOrDefault();
                var allowNegativeStock = companySettings?.AllowNegativeStock ?? false;
                var deductions = new List<(int productId, decimal quantity, int warehouseId, decimal balanceStock)>();
                foreach (var group in sale.Details
                    .GroupBy(d => (d.ProductId, WarehouseId: d.WarehouseId.GetValueOrDefault()))
                    .Select(g => (g.Key.ProductId, g.Key.WarehouseId, Quantity: g.Sum(d => d.Quantity))))
                {
                    var product = await _uow.Products.GetByIdAsync(group.ProductId)
                        ?? throw new CustomException(new Onion.Common.Models.Error
                        {
                            Code = "NOT_FOUND",
                            Message = $"Product {group.ProductId} was not found.",
                            Language = "EN"
                        });

                    var productType = product.ProductTypeId > 0 ? await _uow.ProductTypes.GetByIdAsync(product.ProductTypeId) : null;
                    var isService = product.IsService
                        || string.Equals(productType?.Description?.Trim(), "Servicio", StringComparison.OrdinalIgnoreCase)
                        || (product.Barcode?.StartsWith("SERV-", StringComparison.OrdinalIgnoreCase) ?? false)
                        || (product.Reference?.StartsWith("SERV-", StringComparison.OrdinalIgnoreCase) ?? false);

                    if (isService || product.InvoiceWithoutStock)
                        continue;

                    var warehouseId = group.WarehouseId;
                    decimal availableStock;
                    bool deducted;

                    if (warehouseId > 0)
                    {
                        var inventory = await _uow.Inventories.GetByProductAndWarehouseAsync(group.ProductId, warehouseId);
                        availableStock = inventory?.Quantity ?? 0m;
                        deducted = await _uow.Inventories.TryReduceStockAsync(group.ProductId, warehouseId, group.Quantity);
                    }
                    else
                    {
                        availableStock = product.Stock;
                        deducted = await _uow.Products.TryReduceStockAsync(group.ProductId, group.Quantity);
                    }

                    if (!deducted)
                    {
                        var currentStock = warehouseId > 0
                            ? (await _uow.Inventories.GetByProductAndWarehouseAsync(group.ProductId, warehouseId))?.Quantity ?? 0m
                            : (await _uow.Products.GetByIdAsync(group.ProductId))?.Stock ?? availableStock;

                        throw new CustomException(new Onion.Common.Models.Error
                        {
                            Code = "PRODUCT_OUT_OF_STOCK",
                            Message = $"El producto '{product.Description}' (ID: {product.Id}) no cuenta con disponibilidad en inventario. Stock actual: {currentStock}, solicitado: {group.Quantity}.",
                            Language = "ES",
                            ProductId = product.Id,
                            AvailableStock = currentStock,
                            RequestedQuantity = group.Quantity,
                            Details = new List<Onion.Common.Models.ValidationError>
                            {
                                new() { Field = "productId", Message = product.Id.ToString() },
                                new() { Field = "availableStock", Message = currentStock.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                                new() { Field = "requestedQuantity", Message = group.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                            }
                        });
                    }

                    deductions.Add((group.ProductId, group.Quantity, warehouseId, Math.Max(0m, availableStock - group.Quantity)));
                }

                if (!sale.CashSessionId.HasValue || !sale.CashRegisterId.HasValue)
                {
                    var openSession = (await _uow.CashSessions.FindAsync(s => s.CompanyId == sale.CompanyId && s.Status == Onion.Domain.Finance.CashSessionStatus.Open))
                        .OrderByDescending(s => s.OpenedAt).FirstOrDefault();
                    if (openSession != null)
                    {
                        sale.CashSessionId ??= openSession.Id;
                        sale.CashRegisterId ??= openSession.CashRegisterId;
                    }
                }

                await _uow.Sales.AddAsync(sale);
                await _uow.SaveChangesAsync();

                foreach (var payment in paymentLines)
                {
                    payment.SaleId = sale.Id;
                    payment.CompanyId = sale.CompanyId;
                    payment.CashSessionId = sale.CashSessionId;
                    await _uow.Payments.AddAsync(payment);

                    if (payment.PaymentMethod == PaymentMethod.CASH && sale.CashSessionId.HasValue)
                    {
                        await _uow.CashMovements.AddAsync(new CashMovement
                        {
                            CashRegisterId = sale.CashRegisterId ?? 0,
                            Amount = payment.Amount,
                            Description = $"Venta #{sale.Id}",
                            CompanyId = sale.CompanyId,
                            CashSessionId = sale.CashSessionId,
                            Type = Onion.Domain.Finance.CashMovementType.Sale,
                            Reason = $"Venta #{sale.Id}"
                        });
                    }
                }

                if (sale.PaymentType == PaymentType.CREDIT)
                {
                    await _uow.AccountReceivables.AddAsync(new AccountReceivable
                    {
                        CompanyId = sale.CompanyId,
                        CustomerId = sale.CustomerId,
                        SaleId = sale.Id,
                        TotalAmount = sale.Total,
                        PaidAmount = sale.PaidAmount,
                        DueDate = sale.DueDate ?? sale.Date.AddDays(30),
                        Status = sale.PaidAmount >= sale.Total ? ReceivableStatus.Paid : ReceivableStatus.Open
                    });
                }

                string? ncf = null;
                if (_fiscalSequenceService is not null)
                {
                    try
                    {
                        ncf = await _fiscalSequenceService.NextFiscalNumberAsync(sale.CompanyId, sale.VoucherType);
                    }
                    catch (FiscalSequenceExhaustedException ex)
                    {
                        _logger.LogWarning(ex,
                            "Fiscal sequence {VoucherType} is unavailable for company {CompanyId}. The sale will be created without an NCF until a sequence is configured.",
                            sale.VoucherType, sale.CompanyId);
                    }
                    catch (FiscalSequenceExpiredException ex)
                    {
                        _logger.LogWarning(ex,
                            "Fiscal sequence {VoucherType} is expired for company {CompanyId}. The sale will be created without an NCF until a sequence is configured.",
                            sale.VoucherType, sale.CompanyId);
                    }
                }

                sale.InvoiceFolio = ncf ?? string.Empty;
                var fiscalDocument = Onion.Domain.Invoices.FiscalDocument.ForSale(sale.CompanyId, sale.Id, ncf);
                await _uow.FiscalDocuments.AddAsync(fiscalDocument);

                int performedBy = 0;
                if (!string.IsNullOrWhiteSpace(sale.CreateBy) && int.TryParse(sale.CreateBy, out var parsed))
                    performedBy = parsed;

                foreach (var deduction in deductions)
                {
                    await _uow.InventoryMovements.AddAsync(new Onion.Domain.Inventory.InventoryMovement
                    {
                        CompanyId = sale.CompanyId,
                        Type = Onion.Domain.Inventory.MovementType.Sale,
                        ProductId = deduction.productId,
                        Quantity = deduction.quantity,
                        WarehouseId = deduction.warehouseId,
                        BalanceStock = deduction.balanceStock,
                        PerformedByUserId = performedBy,
                        Reference = sale.InvoiceFolio,
                        Comment = "Sale deduction."
                    });
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
            var sale = await GetByIdAsync(saleId) ?? throw new CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Sale not found", Language = "ES" });
            if (sale.Status == SaleStatus.CANCELLED)
            {
                return;
            }

            if (sale.Status == SaleStatus.PAID || sale.Status == SaleStatus.PARTIAL || sale.Status == SaleStatus.PENDING)
            {
                // Reverse stock for each sale detail
                if (sale.Details != null)
                {
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
                }

                // Adjust cash register / cash session balance
                try
                {
                    if (sale.CashSessionId.HasValue || sale.CashRegisterId.HasValue)
                    {
                        if (sale.PaymentType != PaymentType.CREDIT && sale.Total > 0m)
                        {
                            var cm = new CashMovement
                            {
                                CashRegisterId = sale.CashRegisterId ?? 0,
                                Amount = -sale.Total,
                                Description = $"Anulación de Venta #{sale.Id}: {reason}",
                                CompanyId = sale.CompanyId,
                                CashSessionId = sale.CashSessionId,
                                Type = Onion.Domain.Finance.CashMovementType.Refund,
                                Reason = $"Anulación de Venta #{sale.Id}: {reason}"
                            };
                            await _uow.CashMovements.AddAsync(cm);
                        }
                    }
                }
                catch { }

                sale.Status = SaleStatus.CANCELLED;
                sale.CancellationReason = reason;
                sale.CancelledAt = DateTime.UtcNow;
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
            payment.CompanyId = sale.CompanyId;
            payment.CashSessionId ??= sale.CashSessionId;
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
            if (cashRegisterId.HasValue && payment.PaymentMethod == PaymentMethod.CASH && payment.Amount > 0)
            {
                try
                {
                    var cm = new CashMovement
                    {
                        CashRegisterId = cashRegisterId.Value,
                        Amount = payment.Amount,
                        Description = $"Payment for sale {saleId}",
                        CompanyId = sale.CompanyId,
                        CashSessionId = payment.CashSessionId,
                        Type = CashMovementType.Sale,
                        Reason = "Sale payment",
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
