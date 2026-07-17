using System;

namespace Core.Models.AuthModels
{
    /// <summary>
    /// Represents a permanently banned email address
    /// Used to prevent re-registration after admin ban
    /// </summary>
    public class BannedEmail
    {
        public int Id { get; set; }

        /// <summary>
        /// The banned email address (normalized lowercase)
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// When the email was banned
        /// </summary>
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Admin user ID who banned this email
        /// </summary>
        public int BannedByUserId { get; set; }

        /// <summary>
        /// Admin who banned the email
        /// </summary>
        public User? BannedBy { get; set; }

        /// <summary>
        /// Reason for banning (optional)
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Original user ID that was banned (for reference)
        /// </summary>
        public int? OriginalUserId { get; set; }

        /// <summary>
        /// Original username (for reference)
        /// </summary>
        public string? OriginalUsername { get; set; }
    }
}