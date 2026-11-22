using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.Domain;
using Onion.Domain.Quotations;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IQuotationService
    {
        Task<QuotationListResponseDto> GetPagedQuotationsAsync(
            string? status = null,
            string? search = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int pageNumber = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default);

        Task<QuotationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<QuotationDto> CreateAsync(CreateQuotationDto dto, CancellationToken cancellationToken = default);
        Task<QuotationDto> UpdateStatusAsync(int id, QuotationStatus status, CancellationToken cancellationToken = default);
        Task<Sale> ConvertToSaleAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
