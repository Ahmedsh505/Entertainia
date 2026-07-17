using Core.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Core.Models.EventModels;
using Core.ValidationAttributes;

namespace Entertania1.ViewModels.Event
{
    public class EventCreateVM
    {
        public EventBase Event { get; set; } = new EventBase();

        public IEnumerable<Core.Models.User>? Organizers { get; set; } = new List<Core.Models.User>();
        public IEnumerable<Core.Models.Venue>? Venues { get; set; } = new List<Core.Models.Venue>();

        // Sub models for each category
        public Concert? Concert { get; set; }
        public CulturalEvent? CulturalEvent { get; set; }
        public SportEvent? SportEvent { get; set; }
        public TheaterPlay? TheaterPlay { get; set; }
        public TourismActivity? TourismActivity { get; set; }

        // ✅ NEW: Discount Codes Management
        public List<DiscountCodeVM> DiscountCodes { get; set; } = new List<DiscountCodeVM>();

        // ✅ NEW: For adding new discount code during create/edit
        public DiscountCodeVM? NewDiscountCode { get; set; }

        // ✅ NEW: Ticket tiers (automatically created based on venue capacity)
        public bool AutoCreateTicketTiers { get; set; } = true;

    }
}