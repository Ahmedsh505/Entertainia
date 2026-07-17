using Core.Enums;
using Core.Models.BookingModels;
using Core.Models.PhotoModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.EventModels
{
    public class EventBase
    {
        public int EventId { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        // Venue
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }  // EF sets

        // Organizer (creator) - FK -> ApplicationUser.Id
        public int OrganizerId { get; set; }
        public User? Organizer { get; set; }  // EF sets

        public string? Category { get; set; }

        // ✅ Pricing fields (Base price for Regular tier)
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required]
        public Currency Currency { get; set; } = Currency.EGP;

        // Approval & audit fields
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

        public int? ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNote { get; set; }

        // Convenience computed property
        public bool IsPublished => ApprovalStatus == ApprovalStatus.Approved;

        // ✅ NEW: Ticket Tiers
        public ICollection<EventTicketTier> TicketTiers { get; set; } = new List<EventTicketTier>();

        // ✅ NEW: Discount Codes
        public ICollection<DiscountCode> DiscountCodes { get; set; } = new List<DiscountCode>();

        // Initialize collection
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<EventPhoto> Photos { get; set; } = new List<EventPhoto>();
    }
}