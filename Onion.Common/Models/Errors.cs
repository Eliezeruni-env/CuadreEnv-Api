namespace Onion.Common.Models
{
    public class Error
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? RequestedQuantity { get; set; }
        // Optional structured details for validation or field-level errors
        public System.Collections.Generic.List<ValidationError>? Details { get; set; }
    }

    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}