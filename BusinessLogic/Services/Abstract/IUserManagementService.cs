using System.Threading.Tasks;
using Onion.Common.Models.Pagination;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IUserManagementService
    {
        Task<PagedResult<UserListDto>> GetPagedAsync(int pageNumber, int pageSize, string? q, string? role, bool? active, int? companyId);
        Task<UserDetailDto?> GetByIdAsync(int id);
        Task<UserDetailDto> CreateAsync(CreateUserRequest req, string performedByUserId, CancellationToken cancellationToken = default);
        Task UpdateAsync(int id, UpdateUserRequest req, string performedByUserId);
        Task ToggleActiveAsync(int id, bool active, string performedByUserId);
        Task<string> ResetPasswordAsync(int id, ResetPasswordRequest req, string performedByUserId);
        Task DeleteAsync(int id, string performedByUserId);
        Task<bool> ExistsEmailAsync(string email, int? excludeId = null);
        Task<bool> ExistsUserNameAsync(string userName, int? excludeId = null);
    }
}
