using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Core.Enums;
using Core.Models.BookingModels;
using Core.Models.EventModels;

namespace Core.Models.TicketModels
{
    /// <summary>
    /// Individual ticket entity with QR code support
    /// Each booking generates N tickets where N = NumberOfTickets
    /// </summary>
    public class Ticket
    {
        public int TicketId { get; set; }

        // Relationships
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        public int EventId { get; set; }
        public EventBase? Event { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Ticket Details
        [Required]
        [Range(1, int.MaxValue)]
        public int TicketNumber { get; set; } // 1, 2, 3, etc. (within the booking)

        [Required]
        public TicketTier Tier { get; set; }

        [Required]
        [StringLength(500)]
        public string QRCodeData { get; set; } = string.Empty; // Format: {TicketId}|{BookingId}|{EventId}|{Hash}

        [Required]
        [StringLength(100)]
        public string VerificationHash { get; set; } = string.Empty; // SHA256 hash for security

        // Status
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Used, Cancelled, Expired

        public bool IsScanned { get; set; } = false;
        public DateTime? FirstScannedAt { get; set; }
        public DateTime? LastScannedAt { get; set; }
        public int ScanCount { get; set; } = 0;

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Generate QR code data and verification hash
        /// </summary>
        public void GenerateQRCodeData(string secretKey, string baseUrl)
        {
            // ✅ CRITICAL: Validate baseUrl is provided
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl cannot be null or empty", nameof(baseUrl));
            }

            // Format: TicketId|BookingId|EventId|ExpiryTimestamp
            var dataToHash = $"{TicketId}|{BookingId}|{EventId}|{ExpiresAt?.Ticks ?? 0}|{secretKey}";
            VerificationHash = ComputeSHA256Hash(dataToHash);

            // ✅ FIXED: Remove hardcoded URL, ensure proper URL construction
            // Remove any trailing slashes from baseUrl to avoid double slashes
            var cleanBaseUrl = baseUrl.TrimEnd('/');

            // QR Code contains full verification URL
            QRCodeData = $"{cleanBaseUrl}/Ticket/Verify/{TicketId}?hash={Uri.EscapeDataString(VerificationHash)}";
        }

        /// <summary>
        /// Verify if the ticket is valid
        /// </summary>
        public bool IsValid()
        {
            if (Status != "Active") return false;
            if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow) return false;
            return true;
        }

        /// <summary>
        /// Mark ticket as scanned
        /// </summary>
        public void MarkAsScanned()
        {
            if (!IsScanned)
            {
                IsScanned = true;
                FirstScannedAt = DateTime.UtcNow;
            }

            LastScannedAt = DateTime.UtcNow;
            ScanCount++;
        }

        /// <summary>
        /// Get ticket status badge class for UI
        /// </summary>
        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                "Active" => "badge bg-success",
                "Used" => "badge bg-info",
                "Cancelled" => "badge bg-danger",
                "Expired" => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }

        /// <summary>
        /// Compute SHA256 hash
        /// </summary>
        private static string ComputeSHA256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify QR code hash
        /// </summary>
        public static bool VerifyHash(int ticketId, int bookingId, int eventId, long expiryTicks, string hash, string secretKey)
        {
            var dataToHash = $"{ticketId}|{bookingId}|{eventId}|{expiryTicks}|{secretKey}";
            var computedHash = ComputeSHA256Hash(dataToHash);
            return computedHash.Equals(hash, StringComparison.Ordinal);
        }
    }
}