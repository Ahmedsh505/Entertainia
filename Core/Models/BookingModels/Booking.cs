using System;
using System.ComponentModel.DataAnnotations;
using Core.Enums;
using Core.Models.EventModels;
using Core.Models.PaymentModels;

namespace Core.Models.BookingModels
{
    public class Booking
    {
        public int BookingId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; } // EF will set this

        public int EventId { get; set; }
        public EventBase? Event { get; set; }  // EF will set this

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime BookingDate { get; set; } = DateTime.Now;

        public int NumberOfTickets { get; set; }
        public string? Status { get; set; }  // Pending, Confirmed, Cancelled

        // ✅ Price snapshot at booking time
        [Required]
        public decimal PricePerTicket { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [Required]
        public Currency Currency { get; set; }

        // ✅ NEW: Ticket Tier
        public TicketTier TicketTier { get; set; } = TicketTier.Regular;

        // ✅ NEW: Discount applied
        public int? DiscountCodeId { get; set; }
        public DiscountCode? DiscountCode { get; set; }

        public decimal DiscountAmount { get; set; } = 0;

        // ✅ NEW: Tax (14% VAT)
        public decimal TaxAmount { get; set; } = 0;
        public decimal TotalWithTax { get; set; } = 0;

        // ✅ NEW: Payment relationship
        public Payment? Payment { get; set; }

        // ✅ NEW: Refund relationship
        public Refund? Refund { get; set; }

        // Calculate tax and total
        public void CalculateTax(decimal vatPercentage = 14m)
        {
            var priceAfterDiscount = TotalPrice - DiscountAmount;
            TaxAmount = priceAfterDiscount * (vatPercentage / 100m);
            TotalWithTax = priceAfterDiscount + TaxAmount;
        }
    }
}