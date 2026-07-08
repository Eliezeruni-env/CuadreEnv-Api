using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Onion.Domain.Products
{
    public class Product : BaseEntity
    {
        #region General
        [MaxLength(200)]
        public string Description { get; set; }
        [MaxLength(100)]
        public string Barcode { get; set; }
        public string ShortDescription { get; set; }
        public string Reference { get; set; }
        public int MaximumQuantity { get; set; }
        public int MinimumQuantity { get; set; }

        #endregion

        public int ProductTypeId { get; set; }
        public int CategoryId { get; set; }
        public int CompanyId { get; set; }
        public int UnitOfMeasurementId { get; set; }

        public DateTime? ExpirationDate { get; set; }
        public bool InvoiceWithoutStock { get; set; }

        #region Cost
        public double Cost { get; set; }
        // Current stock quantity available for sale
        public decimal Stock { get; set; }
        // Quantity reserved (not yet deducted from stock until sale is confirmed)
        public decimal ReservedStock { get; set; }
        #endregion
    }
}