namespace Onion.BussinesLogic.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public int CompanyId { get; set; }
        public decimal Cost { get; set; }
        public decimal Stock { get; set; }
        public string? ShortDescription { get; set; }
        public string? Reference { get; set; }
        public int MaximumQuantity { get; set; }
        public int MinimumQuantity { get; set; }
        public int? ProductTypeId { get; set; }
        public int? CategoryId { get; set; }
        public int UnitOfMeasurementId { get; set; }
        public bool InvoiceWithoutStock { get; set; }
    }
}