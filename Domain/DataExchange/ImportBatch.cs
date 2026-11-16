using System;

namespace Onion.Domain.DataExchange
{
    public enum ImportBatchStatus
    {
        Previewed = 0,
        Applied = 1,
        Expired = 2,
        Failed = 3
    }

    public enum ImportMode
    {
        CreateAndUpdate = 0,
        OnlyCreate = 1
    }

    public class ImportBatch : BaseEntity
    {
        public Guid BatchId { get; set; } = Guid.NewGuid();
        public int CompanyId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public ImportMode Mode { get; set; } = ImportMode.CreateAndUpdate;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileHashSha256 { get; set; } = string.Empty;
        public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Previewed;

        public int TotalRows { get; set; }
        public int RowsToInsert { get; set; }
        public int RowsToUpdate { get; set; }
        public int RowsUnchanged { get; set; }
        public int RowsWithErrors { get; set; }

        public string StoredRowsJson { get; set; } = string.Empty;
        public string? ErrorsJson { get; set; }

        public string PreviewedByUserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
        public string? AppliedByUserId { get; set; }
        public DateTime? AppliedAt { get; set; }
        public string? ExecutionResultJson { get; set; }
    }

    public class DataExchangeLog : BaseEntity
    {
        public int CompanyId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string Action { get; set; } = string.Empty; // "IMPORT", "EXPORT", "TEMPLATE"
        public string EntityType { get; set; } = string.Empty; // "PRODUCTS", "CUSTOMERS", etc.
        public string Format { get; set; } = "XLSX"; // "XLSX", "CSV"
        public Guid? BatchId { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string? Details { get; set; }
    }
}
