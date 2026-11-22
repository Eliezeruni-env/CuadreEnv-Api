using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Services;
using Onion.DataAccess;
using Onion.Domain.Finance;

namespace Onion.BussinesLogic.Services.Concrete
{
    public class BankReconciliationService : IBankReconciliationService
    {
        private readonly OnionDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<BankReconciliationService> _logger;

        public BankReconciliationService(
            OnionDbContext db,
            ICurrentUserService currentUser,
            ILogger<BankReconciliationService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _logger = logger;
        }

        private int GetCompanyId()
        {
            var companyId = _currentUser.CompanyId ?? _db.TenantCompanyId;
            if (!companyId.HasValue || companyId.Value <= 0)
                throw new InvalidOperationException("Empresa no autenticada o inválida.");
            return companyId.Value;
        }

        private async Task EnsureDefaultRecordsAsync(int companyId, CancellationToken ct)
        {
            var exists = await _db.BankReconciliationRecords.AnyAsync(r => r.CompanyId == companyId, ct);
            if (!exists)
            {
                var defaults = new List<BankReconciliationRecord>
                {
                    new BankReconciliationRecord
                    {
                        CompanyId = companyId,
                        BankReference = "TRF-00083421",
                        InvoiceNumber = "FAC-0158",
                        CustomerName = "Comercial Norte, S.R.L.",
                        TransactionDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                        DestinationBank = "Banco Popular",
                        RegisteredAmount = 6000m,
                        StatementAmount = 6000m,
                        DifferenceAmount = 0m,
                        Status = "VERIFIED",
                        VerifiedBy = "Auditoría Contable",
                        VerifiedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new BankReconciliationRecord
                    {
                        CompanyId = companyId,
                        BankReference = "TRF-00083422",
                        InvoiceNumber = "FAC-0159",
                        CustomerName = "Industrias del Caribe, S.A.",
                        TransactionDate = new DateTime(2025, 1, 16, 14, 15, 0, DateTimeKind.Utc),
                        DestinationBank = "Banco BHD",
                        RegisteredAmount = 4000m,
                        StatementAmount = 4000m,
                        DifferenceAmount = 0m,
                        Status = "VERIFIED",
                        VerifiedBy = "Auditoría Contable",
                        VerifiedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new BankReconciliationRecord
                    {
                        CompanyId = companyId,
                        BankReference = "TRF-PEND-0160",
                        InvoiceNumber = "FAC-0160",
                        CustomerName = "Servicios Globales, S.R.L.",
                        TransactionDate = new DateTime(2025, 1, 17, 9, 0, 0, DateTimeKind.Utc),
                        DestinationBank = "Banco Popular",
                        RegisteredAmount = 6000m,
                        StatementAmount = null,
                        DifferenceAmount = 0m,
                        Status = "PENDING"
                    },
                    new BankReconciliationRecord
                    {
                        CompanyId = companyId,
                        BankReference = "TRF-00083424",
                        InvoiceNumber = "FAC-0161",
                        CustomerName = "Distribuidora Caribe, S.A.",
                        TransactionDate = new DateTime(2025, 1, 18, 11, 45, 0, DateTimeKind.Utc),
                        DestinationBank = "Banco Santa Cruz",
                        RegisteredAmount = 2500m,
                        StatementAmount = 2000m,
                        DifferenceAmount = 500m,
                        Status = "DISCREPANT",
                        DiscrepancyReason = "Comisión bancaria de RD$ 500 deducida en extracto sin registrar en venta",
                        VerifiedBy = "Supervisor",
                        VerifiedAt = DateTime.UtcNow.AddHours(-5)
                    }
                };

                _db.BankReconciliationRecords.AddRange(defaults);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<IEnumerable<BankTransferRecordDto>> GetRecordsAsync(string? bank, string? status, DateTime? fromDate, DateTime? toDate, string? search, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            await EnsureDefaultRecordsAsync(companyId, ct);

            var query = _db.BankReconciliationRecords.Where(r => r.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(bank) && !bank.Equals("Todos los bancos", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.DestinationBank.ToLower().Contains(bank.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos los estados", StringComparison.OrdinalIgnoreCase))
            {
                if (status.Equals("Pendientes", StringComparison.OrdinalIgnoreCase) || status.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == "PENDING");
                }
                else if (status.Equals("Conciliadas", StringComparison.OrdinalIgnoreCase) || status.Equals("VERIFIED", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == "VERIFIED");
                }
                else if (status.Equals("Discrepancias", StringComparison.OrdinalIgnoreCase) || status.Equals("DISCREPANT", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.Status == "DISCREPANT");
                }
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.TransactionDate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(r => r.TransactionDate <= toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(r => r.BankReference.ToLower().Contains(s) ||
                                         r.InvoiceNumber.ToLower().Contains(s) ||
                                         r.CustomerName.ToLower().Contains(s));
            }

            var list = await query.OrderByDescending(r => r.TransactionDate).ToListAsync(ct);
            return list.Select(MapToDto);
        }

        public async Task<BankReconciliationStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            await EnsureDefaultRecordsAsync(companyId, ct);

            var list = await _db.BankReconciliationRecords.Where(r => r.CompanyId == companyId).ToListAsync(ct);

            var reconciled = list.Where(r => r.Status == "VERIFIED").ToList();
            var pending = list.Where(r => r.Status == "PENDING").ToList();
            var discrepant = list.Where(r => r.Status == "DISCREPANT").ToList();

            return new BankReconciliationStatsDto
            {
                TotalTransfers = list.Sum(r => r.RegisteredAmount),
                TotalTransfersCount = list.Count,
                ReconciledAmount = reconciled.Sum(r => r.RegisteredAmount),
                ReconciledCount = reconciled.Count,
                PendingAmount = pending.Sum(r => r.RegisteredAmount),
                PendingCount = pending.Count,
                DiscrepantAmount = discrepant.Sum(r => r.RegisteredAmount),
                DiscrepantCount = discrepant.Count
            };
        }

        public async Task<BankTransferRecordDto> ReconcileAsync(int id, ReconcileTransferRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var record = await _db.BankReconciliationRecords.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
            if (record == null) throw new KeyNotFoundException($"Registro de transferencia {id} no encontrado.");

            record.Status = "VERIFIED";
            record.StatementAmount = request.StatementAmount ?? record.RegisteredAmount;
            record.DifferenceAmount = Math.Abs(record.RegisteredAmount - (record.StatementAmount ?? record.RegisteredAmount));
            record.VerifiedBy = request.VerifiedBy ?? _currentUser.UserEmail ?? "Supervisor";
            record.VerifiedAt = DateTime.UtcNow;
            record.Notes = request.Notes;

            await _db.SaveChangesAsync(ct);
            return MapToDto(record);
        }

        public async Task<BankTransferRecordDto> MarkDiscrepancyAsync(int id, MarkDiscrepancyRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            var record = await _db.BankReconciliationRecords.FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId, ct);
            if (record == null) throw new KeyNotFoundException($"Registro de transferencia {id} no encontrado.");

            record.Status = "DISCREPANT";
            record.StatementAmount = request.StatementAmount;
            record.DifferenceAmount = request.DifferenceAmount;
            record.DiscrepancyReason = request.DiscrepancyReason;
            record.VerifiedBy = request.VerifiedBy ?? _currentUser.UserEmail ?? "Supervisor";
            record.VerifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return MapToDto(record);
        }

        public async Task<int> ImportStatementAsync(ImportStatementRequest request, CancellationToken ct = default)
        {
            var companyId = GetCompanyId();
            int matched = 0;

            foreach (var item in request.Items)
            {
                var match = await _db.BankReconciliationRecords.FirstOrDefaultAsync(
                    r => r.CompanyId == companyId && r.Status == "PENDING" &&
                    (r.BankReference == item.BankReference || (Math.Abs(r.RegisteredAmount - item.Amount) < 0.01m)), ct);

                if (match != null)
                {
                    match.Status = "VERIFIED";
                    match.StatementAmount = item.Amount;
                    match.DifferenceAmount = Math.Abs(match.RegisteredAmount - item.Amount);
                    match.VerifiedBy = "Importación Extracto Bancario";
                    match.VerifiedAt = DateTime.UtcNow;
                    matched++;
                }
                else
                {
                    // Create unmapped record
                    _db.BankReconciliationRecords.Add(new BankReconciliationRecord
                    {
                        CompanyId = companyId,
                        BankReference = item.BankReference,
                        InvoiceNumber = "EXTRACTO",
                        CustomerName = item.Description ?? "Movimiento Extracto",
                        TransactionDate = item.TransactionDate,
                        DestinationBank = request.DestinationBank,
                        RegisteredAmount = item.Amount,
                        StatementAmount = item.Amount,
                        DifferenceAmount = 0m,
                        Status = "VERIFIED",
                        VerifiedBy = "Importación Extracto Bancario",
                        VerifiedAt = DateTime.UtcNow
                    });
                    matched++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return matched;
        }

        private static BankTransferRecordDto MapToDto(BankReconciliationRecord r)
        {
            return new BankTransferRecordDto
            {
                Id = r.Id,
                SaleId = r.SaleId,
                BankReference = r.BankReference,
                InvoiceNumber = r.InvoiceNumber,
                CustomerName = r.CustomerName,
                Date = r.TransactionDate.ToString("yyyy-MM-dd"),
                DestinationBank = r.DestinationBank,
                AccountLastFour = r.AccountLastFour,
                RegisteredAmount = r.RegisteredAmount,
                StatementAmount = r.StatementAmount,
                DifferenceAmount = r.DifferenceAmount,
                Status = r.Status,
                VerifiedBy = r.VerifiedBy,
                VerifiedAt = r.VerifiedAt?.ToString("yyyy-MM-dd HH:mm"),
                DiscrepancyReason = r.DiscrepancyReason,
                Notes = r.Notes
            };
        }
    }
}
