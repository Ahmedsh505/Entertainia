using Core.Enums;
using Core.Models.BookingModels;
using System;

namespace Core.Models
{
    public class PaymentHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int BookingId { get; set; }

        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }

        public string? PaymentMethod { get; set; }  // Visa, Wallet, etc.
        public string? TransactionId { get; set; }  // Gateway transaction id
        public string? GatewayResponse { get; set; } // Optional JSON or text

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; }
        public Booking Booking { get; set; }
    }
}
