namespace Onion.Domain.Requests;

public sealed class ProcessedRequest
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    public int? UserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string ResponseJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}
