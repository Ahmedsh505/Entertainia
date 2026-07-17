using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Entertania1.ViewModels.Book
{
    public class BookEventViewModel
    {
        public int EventId { get; set; }
        public string? EventName { get; set; }
        public string? VenueName { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime EventDate { get; set; } = DateTime.Now;

        public int VenueCapacity { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please select number of tickets")]
        [Range(1, 100, ErrorMessage = "Number of tickets must be between 1 and 100")]
        public int NumberOfTickets { get; set; } = 1;

        // ✅ NEW: Ticket Tier Selection
        [Required(ErrorMessage = "Please select a ticket tier")]
        public TicketTier SelectedTier { get; set; } = TicketTier.Regular;

        public List<TicketTierOption> AvailableTiers { get; set; } = new List<TicketTierOption>();

        // ✅ NEW: Discount Code
        public string? DiscountCode { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public bool IsDiscountApplied { get; set; } = false;

        // ✅ Pricing fields
        public decimal BasePricePerTicket { get; set; }
        public decimal PricePerTicket { get; set; }
        public Currency Currency { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalPrice { get; set; }

        // ✅ NEW: Currency Conversion (Show estimated price in EGP)
        public decimal? EstimatedPriceInEGP { get; set; }

        // Helper to calculate totals
        public void CalculateTotals(decimal vatPercentage = 14m)
        {
            SubTotal = (PricePerTicket * NumberOfTickets) - DiscountAmount;
            TaxAmount = SubTotal * (vatPercentage / 100m);
            TotalPrice = SubTotal + TaxAmount;
        }
    }

    public class TicketTierOption
    {
        public TicketTier Tier { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
        public decimal PriceMultiplier { get; set; }
        public bool IsAvailable => AvailableSeats > 0;
    }
}