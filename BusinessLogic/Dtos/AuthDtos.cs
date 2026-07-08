namespace Onion.BussinesLogic.Dtos
{
    // RegisterRequestDto extended with optional profile fields to allow richer registration
    // Registration request: companyId removed from required payload.
    // Optional CreateCompanyName allows creating a new company during signup.
    public record RegisterRequestDto(
        string Email,
        string Password,
        string? FirstName = null,
        string? LastName = null,
        string? PhoneNumber = null,
        string? UserName = null,
        string? CreateCompanyName = null
    );


    public record LoginRequestDto(string Email, string Password, string? DeviceId = null);
    public record RefreshRequestDto(string RefreshToken);
    public record RevokeRequestDto(string RefreshToken);
    public record TokenResponseDto(string AccessToken, string RefreshToken);
    public record UserResponseDto(int Id, string Email);

    public record SessionDto(string DeviceId, DateTime CreatedAt, DateTime? LastUsedAt, bool IsActive, DateTime Expires);
}
