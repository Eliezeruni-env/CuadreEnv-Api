using System;

namespace Onion.Domain.Users
{
    public class RefreshToken : BaseEntity
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        // SHA256 hash of the token for secure storage. New tokens will populate TokenHash
        // Existing rows may have Token populated for backward compatibility.
        public string? TokenHash { get; set; }
        public DateTime Expires { get; set; }
        // Optional device identifier to support multi-device sessions
        public string? DeviceId { get; set; }

        // Timestamp of last usage (refresh or introspection) of this refresh token
        public DateTime? LastUsedAt { get; set; }
        // Indicates whether the token has been revoked (by logout or rotation)
        public bool IsRevoked { get; set; }
        // Token value that replaced this token during rotation (if any)
        public string? ReplacedByToken { get; set; }

        public User? User { get; set; }
    }
}
