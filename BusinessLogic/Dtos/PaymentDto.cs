namespace Onion.BussinesLogic.Dtos
{
    public class PaymentDto
    {
        public decimal Amount { get; set; }
        public int SaleId { get; set; }
        public string? Reference { get; set; }
    }
}
