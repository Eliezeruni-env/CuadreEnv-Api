using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain;
using Onion.Domain.Quotations;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class QuotationService : IQuotationService
    {
        private readonly OnionDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<QuotationService> _logger;
        private readonly ISaleService _saleService;

        public QuotationService(
            OnionDbContext db,
            ICurrentUserService currentUserService,
            ILogger<QuotationService> logger,
            ISaleService saleService)
        {
            _db = db;
            _currentUserService = currentUserService;
            _logger = logger;
            _saleService = saleService;
        }

        private int CurrentCompanyId
        {
            get
            {
                var companyId = _currentUserService?.CompanyId ?? _db.TenantCompanyId;
                if (!companyId.HasValue || companyId.Value <= 0)
                    throw new InvalidOperationException("Empresa no autenticada o inválida.");

                return companyId.Value;
            }
        }

        public async Task<QuotationListResponseDto> GetPagedQuotationsAsync(
            string? status = null,
            string? search = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;
            var baseQuery = _db.Quotations
                .Where(q => q.CompanyId == companyId);

            // Compute KPIs across all company quotes
            var allKpis = await baseQuery
                .GroupBy(_ => 1)
                .Select(g => new QuotationSummaryKpiDto
                {
                    TotalCount = g.Count(),
                    DraftCount = g.Count(q => q.Status == QuotationStatus.DRAFT),
                    SentCount = g.Count(q => q.Status == QuotationStatus.SENT),
                    PendingCount = g.Count(q => q.Status == QuotationStatus.DRAFT || q.Status == QuotationStatus.SENT),
                    ApprovedCount = g.Count(q => q.Status == QuotationStatus.APPROVED),
                    ConvertedCount = g.Count(q => q.Status == QuotationStatus.CONVERTED),
                    ExpiredCount = g.Count(q => q.Status == QuotationStatus.EXPIRED)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new QuotationSummaryKpiDto();

            var query = baseQuery.AsQueryable();

            // Status filter
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase) && !status.Equals("todas", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<QuotationStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(q => q.Status == parsedStatus);
                }
                else if (status.Equals("pendientes", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.DRAFT || q.Status == QuotationStatus.SENT);
                }
                else if (status.Equals("borradores", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.DRAFT);
                }
                else if (status.Equals("enviadas", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.SENT);
                }
                else if (status.Equals("aprobadas", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.APPROVED);
                }
                else if (status.Equals("convertidas", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.CONVERTED);
                }
                else if (status.Equals("vencidas", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(q => q.Status == QuotationStatus.EXPIRED);
                }
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(q =>
                    q.QuotationNumber.ToLower().Contains(s) ||
                    (q.CustomerName != null && q.CustomerName.ToLower().Contains(s)) ||
                    (q.Observations != null && q.Observations.ToLower().Contains(s)));
            }

            // Date filter
            if (startDate.HasValue)
            {
                query = query.Where(q => q.IssueDate >= startDate.Value.ToUniversalTime());
            }
            if (endDate.HasValue)
            {
                var endUtc = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                query = query.Where(q => q.IssueDate <= endUtc);
            }

            var totalItems = await query.CountAsync(cancellationToken);
            var pn = Math.Max(1, pageNumber);
            var ps = Math.Clamp(pageSize, 1, 100);

            var items = await query
                .Include(q => q.Details)
                .OrderByDescending(q => q.IssueDate)
                .ThenByDescending(q => q.Id)
                .Skip((pn - 1) * ps)
                .Take(ps)
                .Select(q => MapToDto(q))
                .ToListAsync(cancellationToken);

            return new QuotationListResponseDto
            {
                Kpis = allKpis,
                Items = items,
                TotalItems = totalItems,
                PageNumber = pn,
                PageSize = ps
            };
        }

        public async Task<QuotationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;
            var quotation = await _db.Quotations
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == companyId, cancellationToken);

            return quotation == null ? null : MapToDto(quotation);
        }

        public async Task<QuotationDto> CreateAsync(CreateQuotationDto dto, CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;

            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var count = await _db.Quotations.CountAsync(q => q.CompanyId == companyId, cancellationToken);
            var quotationNumber = $"COT-{(count + 1):D5}";

            var issueDate = dto.IssueDate ?? DateTime.UtcNow;
            var validUntil = dto.ValidUntil ?? issueDate.AddDays(15);

            var quotation = new Quotation
            {
                CompanyId = companyId,
                QuotationNumber = quotationNumber,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                IssueDate = issueDate.ToUniversalTime(),
                ValidUntil = validUntil.ToUniversalTime(),
                PaymentTerms = string.IsNullOrWhiteSpace(dto.PaymentTerms) ? "Contado" : dto.PaymentTerms,
                Salesperson = dto.Salesperson,
                SellerId = dto.SellerId,
                WarehouseId = dto.WarehouseId,
                Observations = dto.Observations,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow,
            };

            decimal subtotalTotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            if (dto.Details != null && dto.Details.Count > 0)
            {
                foreach (var item in dto.Details)
                {
                    var qty = item.Quantity > 0 ? item.Quantity : 1;
                    var unitPrice = item.UnitPrice >= 0 ? item.UnitPrice : 0;
                    var descPct = Math.Clamp(item.DiscountPercent, 0, 100);
                    var lineSubtotal = Math.Round(qty * unitPrice, 2);
                    var discountAmount = Math.Round(lineSubtotal * (descPct / 100m), 2);
                    var taxableAmount = lineSubtotal - discountAmount;
                    var taxPct = item.TaxPercent >= 0 ? item.TaxPercent : 18m;
                    var lineTax = Math.Round(taxableAmount * (taxPct / 100m), 2);
                    var lineTotal = taxableAmount + lineTax;

                    subtotalTotal += lineSubtotal;
                    discountTotal += discountAmount;
                    taxTotal += lineTax;

                    quotation.Details.Add(new QuotationDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductCode = item.ProductCode,
                        Quantity = qty,
                        UnitPrice = unitPrice,
                        DiscountPercent = descPct,
                        Subtotal = lineSubtotal,
                        TaxPercent = taxPct,
                        TaxAmount = lineTax,
                        Total = lineTotal
                    });
                }
            }

            quotation.Subtotal = subtotalTotal;
            quotation.DiscountTotal = discountTotal;
            quotation.TaxTotal = taxTotal;
            quotation.Total = subtotalTotal - discountTotal + taxTotal;

            _db.Quotations.Add(quotation);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return MapToDto(quotation);
        }

        public async Task<QuotationDto> UpdateStatusAsync(int id, QuotationStatus status, CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;
            var quotation = await _db.Quotations
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == companyId, cancellationToken);

            if (quotation == null)
            {
                throw new KeyNotFoundException($"Cotización con ID {id} no encontrada.");
            }

            quotation.Status = status;
            quotation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            return MapToDto(quotation);
        }

        public async Task<Sale> ConvertToSaleAsync(int id, CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;
            var quotation = await _db.Quotations
                .Include(q => q.Details)
                .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == companyId, cancellationToken);

            if (quotation == null)
            {
                throw new KeyNotFoundException($"Cotización con ID {id} no encontrada.");
            }

            if (quotation.Status == QuotationStatus.CONVERTED && quotation.ConvertedSaleId.HasValue)
                return await _saleService.GetByIdAsync(quotation.ConvertedSaleId.Value)
                    ?? throw new InvalidOperationException("La cotización ya está convertida, pero su venta asociada no existe.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Create Sale from Quotation
            var sale = new Sale
            {
                CompanyId = companyId,
                CustomerId = quotation.CustomerId,
                Date = DateTime.UtcNow,
                SubTotal = quotation.Subtotal,
                Discount = quotation.DiscountTotal,
                Tax = quotation.TaxTotal,
                Total = quotation.Total,
                Status = SaleStatus.PENDING,
                PaymentType = quotation.PaymentTerms.Equals("Contado", StringComparison.OrdinalIgnoreCase) ? PaymentType.CASH : PaymentType.CREDIT,
                PaymentMethod = PaymentMethod.CASH,
                OriginChannel = SaleOriginChannel.ADMINISTRATIVO,
                Notes = $"Generado desde Cotización #{quotation.QuotationNumber}. {quotation.Observations}".Trim(),
            };

            foreach (var detail in quotation.Details)
            {
                sale.Details.Add(new SaleDetail
                {
                    ProductId = detail.ProductId,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Discount = Math.Round(detail.Subtotal * (detail.DiscountPercent / 100m), 2),
                    Tax = detail.TaxAmount,
                    WarehouseId = quotation.WarehouseId
                });
            }

            sale = await _saleService.CreateAsync(sale);

            // Update Quotation status to CONVERTED
            quotation.Status = QuotationStatus.CONVERTED;
            quotation.ConvertedSaleId = sale.Id;
            quotation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return sale;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var companyId = CurrentCompanyId;
            var quotation = await _db.Quotations
                .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == companyId, cancellationToken);

            if (quotation == null) return false;

            _db.Quotations.Remove(quotation);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static QuotationDto MapToDto(Quotation q)
        {
            return new QuotationDto
            {
                Id = q.Id,
                CompanyId = q.CompanyId,
                QuotationNumber = q.QuotationNumber,
                CustomerId = q.CustomerId,
                CustomerName = q.CustomerName,
                CustomerEmail = q.CustomerEmail,
                CustomerPhone = q.CustomerPhone,
                IssueDate = q.IssueDate,
                ValidUntil = q.ValidUntil,
                PaymentTerms = q.PaymentTerms,
                Salesperson = q.Salesperson,
                SellerId = q.SellerId,
                WarehouseId = q.WarehouseId,
                Observations = q.Observations,
                Status = q.Status,
                Subtotal = q.Subtotal,
                DiscountTotal = q.DiscountTotal,
                TaxTotal = q.TaxTotal,
                Total = q.Total,
                ConvertedSaleId = q.ConvertedSaleId,
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt,
                Details = q.Details.Select(d => new QuotationDetailDto
                {
                    Id = d.Id,
                    QuotationId = d.QuotationId,
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    ProductCode = d.ProductCode,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    DiscountPercent = d.DiscountPercent,
                    Subtotal = d.Subtotal,
                    TaxPercent = d.TaxPercent,
                    TaxAmount = d.TaxAmount,
                    Total = d.Total,
                    Notes = d.Notes
                }).ToList()
            };
        }
    }
}
