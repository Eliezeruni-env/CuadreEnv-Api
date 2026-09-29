using Microsoft.EntityFrameworkCore;
using Onion.BussinesLogic.Services.Abstract;
using Onion.DataAccess;

namespace Onion.BussinesLogic.Services.Concrete;

public sealed class AccountStatusService : IAccountStatusService
{
    private readonly OnionDbContext _db;
    public AccountStatusService(OnionDbContext db) => _db = db;

    public Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Users.AsNoTracking().IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => u.Active && !u.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);
}
