using System;

namespace Onion.Common.Services
{
    public interface ICurrentUserService
    {
        int? CompanyId { get; }
        int? UserId { get; }
        string? ClerkUserId { get; }
        string? UserEmail { get; }
        string? UserRole { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
        bool IsAuthenticated { get; }
        bool IsGlobalAdministrator { get; }
    }
}
