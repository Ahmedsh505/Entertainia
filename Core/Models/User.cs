using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Core.Enums;
using Core.Models.EventModels;
using Core.Models.AuthModels;
using Core.Models.BookingModels;

namespace Core.Models
{
    // Identity-ready user with integer key + Enhanced Auth features + Profile Picture
    public class User : IdentityUser<int>
    {
        // Personal info
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }

        // ✅ NEW: Profile Picture
        public string? ProfilePicturePath { get; set; }

        // Role (for quick reads; canonical source remains Identity roles)
        public string? Role { get; set; }

        // Account approval status (organizers will start as PendingApproval)
        public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingApproval;

        // Email confirmation (user can login but will see banner)
        public bool EmailConfirmed { get; set; } = false;
        public DateTime? EmailConfirmedAt { get; set; }

        // Two-Factor Authentication (optional for users)
        public bool TwoFactorEnabled { get; set; } = false;
        public DateTime? TwoFactorEnabledAt { get; set; }

        // Security tracking
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LastFailedLoginAt { get; set; }

        // Verification document (organizers)
        public string? VerificationDocumentPath { get; set; }
        public ICollection<VerificationDoc> VerificationDocs { get; set; } = new List<VerificationDoc>();

        // Auth token relationships
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<EmailConfirmationToken> EmailConfirmationTokens { get; set; } = new List<EmailConfirmationToken>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
        public ICollection<TwoFactorCode> TwoFactorCodes { get; set; } = new List<TwoFactorCode>();

        // Navigation
        public ICollection<EventBase> OrganizedEvents { get; set; } = new List<EventBase>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Soft Delete properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public DeletionType DeletionType { get; set; } = DeletionType.None;
        public int? DeletedByUserId { get; set; }
        public User? DeletedBy { get; set; }
        public string? DeletionReason { get; set; }
        public string? OriginalUsername { get; set; }

        // ✅ NEW: Helper method to get profile picture or generate initials avatar
        public string GetProfilePictureOrInitials()
        {
            if (!string.IsNullOrEmpty(ProfilePicturePath))
                return ProfilePicturePath;

            // Return initials for avatar generation
            var firstInitial = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : "";
            return $"{firstInitial}{lastInitial}";
        }

        // ✅ NEW: Check if user has a profile picture
        public bool HasProfilePicture()
        {
            return !string.IsNullOrEmpty(ProfilePicturePath);
        }

        // Helper method to check if user can login
        public bool CanLogin()
        {
            return !IsDeleted &&
                   AccountStatus == AccountStatus.Active &&
                   DeletionType == DeletionType.None;
        }

        // Helper method to check if email is banned
        public bool IsEmailBanned()
        {
            return IsDeleted && DeletionType == DeletionType.AdminBanned;
        }
    }
}