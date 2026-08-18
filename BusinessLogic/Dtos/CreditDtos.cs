namespace Onion.BussinesLogic.Dtos
{
    public record CreditDto(
        int Id,
        int CustomerId,
        decimal TotalAmount,
        decimal PaidAmount,
        decimal Balance,
        System.DateTime DueDate,
        string Status,
        decimal? MinimumPaymentAmount,
        string? PaymentFrequency);

    public record CreateCreditDto(
        int CustomerId,
        decimal TotalAmount,
        System.DateTime DueDate,
        decimal? MinimumPaymentAmount,
        string? PaymentFrequency);

    public record UpdateCreditDto(
        int Id,
        decimal? TotalAmount,
        System.DateTime? DueDate,
        decimal? MinimumPaymentAmount,
        string? PaymentFrequency);

    public record CreateCreditPaymentDto(decimal Amount, string? Notes);
    public record CreditPaymentDto(int Id, int CreditId, decimal Amount, System.DateTime PaidAt, string? Notes);
}
