using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IBankReconciliationService
    {
        Task<IEnumerable<BankTransferRecordDto>> GetRecordsAsync(string? bank, string? status, DateTime? fromDate, DateTime? toDate, string? search, CancellationToken ct = default);
        Task<BankReconciliationStatsDto> GetStatsAsync(CancellationToken ct = default);
        Task<BankTransferRecordDto> ReconcileAsync(int id, ReconcileTransferRequest request, CancellationToken ct = default);
        Task<BankTransferRecordDto> MarkDiscrepancyAsync(int id, MarkDiscrepancyRequest request, CancellationToken ct = default);
        Task<int> ImportStatementAsync(ImportStatementRequest request, CancellationToken ct = default);
    }
}
