using Onion.DataAccess.Repositories.Abstract;
using Onion.Domain.Warehouses;

namespace Onion.DataAccess.Repositories.Concrete
{
    public class MovementRepository : GenericRepository<Movement>, IMovementRepository
    {
        public MovementRepository(OnionDbContext context) : base(context) { }
    }
}
