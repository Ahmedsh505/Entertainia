using System;

namespace Core.Models.AuthModels
{
    /// <summary>
    /// Refresh token for JWT authentication
    /// Allows users to get new access tokens without re-logging in
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }

        // The actual token string (should be unique and cryptographically secure)
        public string Token { get; set; } = string.Empty;

        // User this token belongs to
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Token lifecycle
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // When the token was actually used (null if never used)
        public DateTime? UsedAt { get; set; }

        // Security: track if token was revoked
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReasonRevoked { get; set; }

        // Replaced by chain (when token is refreshed, it gets replaced)
        public string? ReplacedByToken { get; set; }

        // Device/IP tracking
        public string? CreatedByIp { get; set; }
        public string? DeviceInfo { get; set; }

        // Helper properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}