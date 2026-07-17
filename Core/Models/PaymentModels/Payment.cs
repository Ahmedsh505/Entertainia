using System;
using System.ComponentModel.DataAnnotations;
using Core.Enums;
using Core.Models.BookingModels;

namespace Core.Models.PaymentModels
{
    public class Payment
    {
        public int PaymentId { get; set; }

        // Related Booking
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public Currency Currency { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        // Payment Gateway Info
        [StringLength(200)]
        public string? PaymentGateway { get; set; } = "Paymob"; // Default to Paymob

        [StringLength(200)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? PaymentGatewayReference { get; set; }

        // Payment method (Credit Card, Wallet, Cash on Delivery, etc.)
        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        // Error/Failure info
        [StringLength(1000)]
        public string? FailureReason { get; set; }

        // Additional metadata (JSON string for flexibility)
        public string? Metadata { get; set; }

        // Tax calculation (14% VAT)
        public decimal TaxAmount { get; set; }
        public decimal TotalAmountWithTax { get; set; }

        public void CalculateTax(decimal vatPercentage = 14m)
        {
            TaxAmount = Amount * (vatPercentage / 100m);
            TotalAmountWithTax = Amount + TaxAmount;
        }
    }
}