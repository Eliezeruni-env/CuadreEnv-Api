namespace Onion.BussinesLogic.Dtos
{
    public class PaymentDto
    {
        public decimal Amount { get; set; }
        public int SaleId { get; set; }
        public string? Reference { get; set; }
        public string? Method { get; set; }
        public string? PaymentMethod { get; set; }
        public int? CashRegisterId { get; set; }
        public int? CashSessionId { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal? ChangeAmount { get; set; }
    }
}
