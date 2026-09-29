

namespace Onion.DataAccess.Models
{
    public class ProductRow
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal Cost { get; set; }
        public decimal Stock { get; set; }
        public string? Reference { get; set; }
        public string? ShortDescription { get; set; }
    }
}
