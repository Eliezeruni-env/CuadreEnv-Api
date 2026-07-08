using Onion.Domain;

namespace Onion.Domain.Warehouses
{
    public class Warehouse : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
