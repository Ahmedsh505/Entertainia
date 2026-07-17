using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Models.EventModels
{
    /// <summary>
    /// Represents pricing tiers for an event
    /// VIP: 5% capacity at 2.5x price
    /// Special: 15% capacity at 1.5x price
    /// Regular: 80% capacity at base price
    /// </summary>
    public class EventTicketTier
    {
        public int EventTicketTierId { get; set; }

        public int EventId { get; set; }
        public EventBase? Event { get; set; }

        [Required]
        public TicketTier Tier { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PriceMultiplier { get; set; }

        [Required]
        [Range(0.01, 1.0)]
        public decimal CapacityPercentage { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int AvailableSeats { get; set; }

        public bool IsActive { get; set; } = true;

        // Calculated price based on event base price
        public decimal GetPrice(decimal basePrice)
        {
            return basePrice * PriceMultiplier;
        }

        // Helper to get tier name
        public string GetTierName()
        {
            return Tier switch
            {
                TicketTier.VIP => "VIP",
                TicketTier.Special => "Special",
                TicketTier.Regular => "Regular",
                _ => "Unknown"
            };
        }

        // Initialize tiers for an event
        public static List<EventTicketTier> CreateDefaultTiers(int eventId, int venueCapacity)
        {
            return new List<EventTicketTier>
            {
                new EventTicketTier
                {
                    EventId = eventId,
                    Tier = TicketTier.VIP,
                    PriceMultiplier = 2.5m,
                    CapacityPercentage = 0.05m,
                    AvailableSeats = (int)(venueCapacity * 0.05m),
                    IsActive = true
                },
                new EventTicketTier
                {
                    EventId = eventId,
                    Tier = TicketTier.Special,
                    PriceMultiplier = 1.5m,
                    CapacityPercentage = 0.15m,
                    AvailableSeats = (int)(venueCapacity * 0.15m),
                    IsActive = true
                },
                new EventTicketTier
                {
                    EventId = eventId,
                    Tier = TicketTier.Regular,
                    PriceMultiplier = 1.0m,
                    CapacityPercentage = 0.80m,
                    AvailableSeats = (int)(venueCapacity * 0.80m),
                    IsActive = true
                }
            };
        }
    }
}