
namespace Onion.DataAccess.Models
{
    public class FilterPayload
    {
        public string? Description { get; set; }
        public decimal? MinCost { get; set; }
        public decimal? MaxCost { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
