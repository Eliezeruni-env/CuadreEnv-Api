namespace Onion.BussinesLogic.Services.Abstract;

public interface IAccountStatusService
{
    Task<bool> IsActiveAsync(int userId, CancellationToken cancellationToken = default);
}
