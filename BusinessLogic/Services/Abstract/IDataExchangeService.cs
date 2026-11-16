using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.DataExchange;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IDataExchangeService
    {
        Task<(byte[] Content, string ContentType, string FileName)> GenerateTemplateAsync(int companyId, string entityType, string format = "xlsx");
        Task<(byte[] Content, string ContentType, string FileName)> ExportDataAsync(int companyId, string entityType, string format = "xlsx");
        Task<ImportPreviewResponseDto> PreviewImportAsync(int companyId, string entityType, Stream fileStream, string fileName, ImportMode mode, string userId);
        Task<ImportConfirmResponseDto> ConfirmImportAsync(int companyId, Guid batchId, string userId);
        Task<(byte[] Content, string ContentType, string FileName)> ExportErrorsAsync(int companyId, Guid batchId);
        Task<List<DataExchangeLogDto>> GetHistoryAsync(int companyId);
    }
}
