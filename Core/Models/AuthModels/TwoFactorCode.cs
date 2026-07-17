using System;

namespace Core.Models.AuthModels
{
    /// <summary>
    /// 2FA code sent via email
    /// 6-digit code valid for 10 minutes
    /// </summary>
    public class TwoFactorCode
    {
        public int Id { get; set; }

        // The 6-digit code (e.g., "123456")
        public string Code { get; set; } = string.Empty;

        // User this code belongs to
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Code lifecycle (10 minutes validity)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Was this code used?
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }

        // Failed attempt tracking (max 3 attempts)
        public int FailedAttempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;

        // IP tracking
        public string? CreatedByIp { get; set; }
        public string? UsedByIp { get; set; }

        // Helper properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsValid => !IsUsed && !IsExpired && !IsLocked;
    }
}