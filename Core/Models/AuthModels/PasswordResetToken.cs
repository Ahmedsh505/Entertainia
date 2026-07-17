using System;

namespace Core.Models.AuthModels
{
    /// <summary>
    /// Token for password reset requests
    /// Generated when user clicks "Forgot Password"
    /// </summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }

        // The reset token (URL-safe)
        public string Token { get; set; } = string.Empty;

        // User who requested password reset
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Email used for reset (for verification)
        public string Email { get; set; } = string.Empty;

        // Token lifecycle (20 minutes as requested)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Was this token used?
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }

        // IP tracking for security
        public string? CreatedByIp { get; set; }
        public string? UsedByIp { get; set; }

        // Helper properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsValid => !IsUsed && !IsExpired;
    }
}