using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Dtos;

namespace Onion.BussinesLogic.Services.Abstract
{
    public interface IAuthService
    {
        Task<UserResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<TokenResponseDto> LoginAsync(LoginRequestDto request);
        Task<IEnumerable<Onion.BussinesLogic.Dtos.SessionDto>> ListSessionsAsync(int userId);
        Task RevokeSessionAsync(int userId, string deviceId);
        Task RevokeAllExceptCurrentAsync(int userId, string currentDeviceId);
        Task<TokenResponseDto> RefreshTokenAsync(RefreshRequestDto request);
        Task RevokeTokenAsync(RevokeRequestDto request);
        // Revoke all refresh tokens for a given user (logout from all devices)
        Task RevokeAllTokensAsync(int userId);
        // Issue a fresh access + refresh token pair for an existing user (useful after assigning company)
        Task<TokenResponseDto> IssueTokensForUserAsync(int userId, string? deviceId = null);
    }
}
