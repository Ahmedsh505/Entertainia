using System;
using System.ComponentModel.DataAnnotations;
using Core.Enums;
using Core.Models.BookingModels;

namespace Core.Models.PaymentModels
{
    public class Refund
    {
        public int RefundId { get; set; }

        // Related Payment & Booking
        public int PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        // Refund Amount Calculation
        [Required]
        public decimal OriginalAmount { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal RefundPercentage { get; set; } = 80m; // Default 80% refund

        [Required]
        public decimal RefundAmount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        // Status & Processing
        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // User Information
        public int RequestedByUserId { get; set; }
        public User? RequestedBy { get; set; }

        public int? ProcessedByUserId { get; set; }
        public User? ProcessedBy { get; set; }

        // Reason & Notes
        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }

        // Timestamps
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        // Failure Information
        [StringLength(1000)]
        public string? FailureReason { get; set; }

        // Gateway Information
        [StringLength(200)]
        public string? RefundTransactionId { get; set; }

        [StringLength(500)]
        public string? PaymentGatewayReference { get; set; }

        /// <summary>
        /// Calculate refund amount based on original amount and percentage
        /// </summary>
        public void CalculateRefund()
        {
            RefundAmount = OriginalAmount * (RefundPercentage / 100m);
        }

        /// <summary>
        /// Static helper method to calculate refund amount
        /// Used in views and controllers for quick calculations
        /// </summary>
        /// <param name="originalAmount">Original payment amount</param>
        /// <param name="refundPercentage">Percentage to refund (default 80%)</param>
        /// <returns>Calculated refund amount</returns>
        public static decimal CalculateRefundAmount(decimal originalAmount, decimal refundPercentage = 80m)
        {
            if (refundPercentage < 0 || refundPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(refundPercentage), "Refund percentage must be between 0 and 100");
            }

            return Math.Round(originalAmount * (refundPercentage / 100m), 2);
        }

        /// <summary>
        /// Check if refund can be processed
        /// </summary>
        public bool CanBeProcessed()
        {
            return Status == PaymentStatus.Pending &&
                   ProcessedAt == null &&
                   Booking?.Event?.StartDate > DateTime.UtcNow;
        }

        /// <summary>
        /// Check if refund is in final state
        /// </summary>
        public bool IsFinal()
        {
            return Status == PaymentStatus.Completed ||
                   Status == PaymentStatus.Failed ||
                   Status == PaymentStatus.Refunded;
        }
        // ✅ Add these updated methods to your Refund.cs model

        /// <summary>
        /// Get refund status display text
        /// </summary>
        public string GetStatusText()
        {
            return Status switch
            {
                PaymentStatus.Pending => "Pending Admin Review",
                PaymentStatus.Processing => "Processing Refund",
                PaymentStatus.Completed => "Refund Completed",
                PaymentStatus.Refunded => "Refunded",
                PaymentStatus.Failed => "Refund Failed",
                PaymentStatus.RefundRejected => "Refund Rejected",  // ✅ NEW
                _ => "Unknown Status"
            };
        }

        /// <summary>
        /// Get CSS class for status badge
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
                PaymentStatus.RefundRejected => "badge bg-danger",  // ✅ NEW
                _ => "badge bg-secondary"
            };
        }



    }
}