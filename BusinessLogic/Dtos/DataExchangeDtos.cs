using System;
using System.Collections.Generic;

namespace Onion.BussinesLogic.Dtos
{
    public class RowFieldChangeDto
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public class RowErrorDto
    {
        public int RowNumber { get; set; }
        public string Field { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class PreviewRowItemDto
    {
        public int RowNumber { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "INSERT", "UPDATE", "UNCHANGED", "ERROR"
        public string Description { get; set; } = string.Empty;
        public List<RowFieldChangeDto> Changes { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class ImportPreviewResponseDto
    {
        public Guid BatchId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int RowsToInsert { get; set; }
        public int RowsToUpdate { get; set; }
        public int RowsUnchanged { get; set; }
        public int RowsWithErrors { get; set; }
        public bool CanConfirm => RowsWithErrors == 0 && (RowsToInsert > 0 || RowsToUpdate > 0);
        public DateTime ExpiresAt { get; set; }
        public List<PreviewRowItemDto> SampleRows { get; set; } = new();
        public List<RowErrorDto> Errors { get; set; } = new();
    }

    public class ImportConfirmResponseDto
    {
        public Guid BatchId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public bool AlreadyApplied { get; set; }
        public DateTime? AppliedAt { get; set; }
    }

    public class DataExchangeLogDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public Guid? BatchId { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Details { get; set; }
    }
}
