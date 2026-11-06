namespace Onion.DataAccess.Models
{
    public class ProductRow
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public decimal Cost { get; set; }
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        public string? Reference { get; set; }
        public string? ShortDescription { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int ProductTypeId { get; set; }
        public int CompanyId { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsService { get; set; }
        public bool InvoiceWithoutStock { get; set; }
        public int UnitOfMeasurementId { get; set; }
    }
}
