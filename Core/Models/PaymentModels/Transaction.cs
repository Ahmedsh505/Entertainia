using Core.Enums;
using Core.Models.BookingModels;
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.PaymentModels
{
    /// <summary>
    /// Complete transaction log for all financial activities
    /// Replaces and enhances PaymentHistory with better structure
    /// </summary>
    public class Transaction
    {
        public int TransactionId { get; set; }

        // Related entities
        public int UserId { get; set; }
        public User User { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public int? RefundId { get; set; }
        public Refund? Refund { get; set; }

        // Transaction details
        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } // Payment, Refund, Cancellation

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        [Required]
        public PaymentStatus Status { get; set; }

        // Gateway details
        [StringLength(200)]
        public string? PaymentMethod { get; set; } // Visa, Wallet, etc.

        [StringLength(200)]
        public string? PaymentGateway { get; set; } // Paymob

        [StringLength(200)]
        public string? TransactionReference { get; set; } // Our internal transaction ID

        [StringLength(200)]
        public string? GatewayTransactionId { get; set; } // Paymob transaction ID

        [StringLength(500)]
        public string? GatewayReference { get; set; } // Additional gateway reference

        // Response data
        public string? GatewayResponse { get; set; } // JSON response from gateway

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Metadata
        [StringLength(100)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public string? Metadata { get; set; } // JSON for additional data

        /// <summary>
        /// Update transaction status and timestamp
        /// </summary>
        public void UpdateStatus(PaymentStatus newStatus, string? message = null)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;

            if (newStatus == PaymentStatus.Completed || newStatus == PaymentStatus.Refunded)
            {
                CompletedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(message))
            {
                Description = message;
            }
        }

        /// <summary>
        /// Check if transaction is in final state
        /// </summary>
        public bool IsFinal()
        {
            return Status == PaymentStatus.Completed ||
                   Status == PaymentStatus.Refunded ||
                   Status == PaymentStatus.Failed ||
                   Status == PaymentStatus.RefundRejected;
        }

        /// <summary>
        /// Get status badge CSS class for UI
        /// </summary>
        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                PaymentStatus.Pending => "badge bg-warning text-dark",
                PaymentStatus.Processing => "badge bg-info",
                PaymentStatus.Completed => "badge bg-success",
                PaymentStatus.Refunded => "badge bg-success",
                PaymentStatus.Failed => "badge bg-danger",
                PaymentStatus.RefundRejected => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }

        /// <summary>
        /// Get human-readable status text
        /// </summary>
        public string GetStatusText()
        {
            return Status switch
            {
                PaymentStatus.Pending => "Pending",
                PaymentStatus.Processing => "Processing",
                PaymentStatus.Completed => "Completed",
                PaymentStatus.Refunded => "Refunded",
                PaymentStatus.Failed => "Failed",
                PaymentStatus.RefundRejected => "Rejected",
                _ => "Unknown"
            };
        }
    }
}
