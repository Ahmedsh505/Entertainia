using System;

namespace Core.Models.AuthModels
{
    /// <summary>
    /// Token for email confirmation using 6-digit code
    /// Generated when user registers or requests new confirmation code
    /// </summary>
    public class EmailConfirmationToken
    {
        public int Id { get; set; }

        // ✅ NEW: 6-digit verification code (instead of URL token)
        public string Code { get; set; } = string.Empty;

        // ⚠️ DEPRECATED: Keep for backward compatibility, but no longer used
        public string? Token { get; set; }

        // User who needs to confirm their email
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Email that needs to be confirmed
        public string Email { get; set; } = string.Empty;

        // Token lifecycle
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Was this code used?
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }

        // ✅ NEW: Failed verification attempts tracking
        public int FailedAttempts { get; set; } = 0;

        // ✅ NEW: Lock after 5 failed attempts
        public bool IsLocked { get; set; } = false;
        public DateTime? LockedUntil { get; set; }

        // ✅ NEW: Track resend requests
        public int ResendCount { get; set; } = 0;
        public DateTime? LastResendAt { get; set; }

        // IP tracking
        public string? CreatedByIp { get; set; }
        public string? UsedByIp { get; set; }

        // Helper properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public bool IsValid => !IsUsed &&
                              !IsExpired &&
                              !IsLocked &&
                              (LockedUntil == null || DateTime.UtcNow >= LockedUntil);

        // ✅ NEW: Check if user is in lockout period
        public bool IsInLockout => IsLocked && LockedUntil.HasValue && DateTime.UtcNow < LockedUntil.Value;

        // ✅ NEW: Check if user can resend (1-minute cooldown)
        public bool CanResend => !LastResendAt.HasValue ||
                                 (DateTime.UtcNow - LastResendAt.Value).TotalMinutes >= 1;
    }
}