using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Warehouses;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class WarehouseRepository : GenericRepository<Warehouse>, IWarehouseRepository
    {
        public WarehouseRepository(OnionDbContext context) : base(context) { }
    }
}
