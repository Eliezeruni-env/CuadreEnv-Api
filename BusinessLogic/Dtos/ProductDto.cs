namespace Onion.BussinesLogic.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public string? Brand { get; set; }
        public int CompanyId { get; set; }
        public decimal Cost { get; set; } = 0m;
        public decimal Price { get; set; } = 0m;
        public decimal Stock { get; set; } = 0m;
        public string? ShortDescription { get; set; }
        public string? Reference { get; set; }
        public int MaximumQuantity { get; set; }
        public int MinimumQuantity { get; set; } = 0;
        public int? ProductTypeId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public ProductCategoryDto? Category { get; set; }
        public int UnitOfMeasurementId { get; set; }
        public bool InvoiceWithoutStock { get; set; }
        public bool IsService { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsOrganic { get; set; }
        public string? ImageUrl { get; set; }
        public bool AllowFractional { get; set; }
        public decimal? ProfitMargin { get; set; }
    }
    public class ProductCategoryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public record PreloadedProductDto(string Description, string? Barcode, string? Reference, string Category, decimal Cost, decimal Price, decimal Stock, int MinQuantity);
    public record PreloadedCatalogDto(string Name, IReadOnlyList<PreloadedProductDto> Products);

    public sealed class ProductLookupDto
    {
        public string Barcode { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Brand { get; init; }
        public string? Category { get; init; }
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
        public string? Quantity { get; init; }
        public bool IsLocal { get; init; }
    }
}