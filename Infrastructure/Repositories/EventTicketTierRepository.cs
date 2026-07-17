using Core.Enums;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;

namespace Infrastructure.Repositories
{
    public class EventTicketTierRepository : IEventTicketTierRepo
    {
        private readonly EntertaniaDbcontext _context;

        public EventTicketTierRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(EventTicketTier tier)
        {
            _context.Set<EventTicketTier>().Add(tier);
        }

        public void Update(EventTicketTier tier)
        {
            _context.Set<EventTicketTier>().Update(tier);
        }

        public void Delete(EventTicketTier tier)
        {
            _context.Set<EventTicketTier>().Remove(tier);
        }

        public List<EventTicketTier> GetAll()
        {
            return _context.Set<EventTicketTier>()
                .Include(t => t.Event)
                .ToList();
        }

        public EventTicketTier? GetById(int id)
        {
            return _context.Set<EventTicketTier>()
                .Include(t => t.Event)
                .FirstOrDefault(t => t.EventTicketTierId == id);
        }

        public List<EventTicketTier> GetByEvent(int eventId)
        {
            return _context.Set<EventTicketTier>()
                .Where(t => t.EventId == eventId)
                .OrderByDescending(t => t.Tier)
                .ToList();
        }

        public EventTicketTier? GetByEventAndTier(int eventId, TicketTier tier)
        {
            return _context.Set<EventTicketTier>()
                .FirstOrDefault(t => t.EventId == eventId && t.Tier == tier);
        }

        public void UpdateAvailableSeats(int tierId, int seatsToReduce)
        {
            var tier = _context.Set<EventTicketTier>().Find(tierId);
            if (tier != null)
            {
                tier.AvailableSeats -= seatsToReduce;
                if (tier.AvailableSeats < 0) tier.AvailableSeats = 0;
                _context.Set<EventTicketTier>().Update(tier);
            }
        }

        // ✅ NEW: Restore seats (used when booking is cancelled/deleted)
        public void RestoreAvailableSeats(int tierId, int seatsToRestore)
        {
            var tier = _context.Set<EventTicketTier>().Find(tierId);
            if (tier != null)
            {
                // Get the original capacity for this tier
                var originalCapacity = tier.Event?.Venue != null
                    ? (int)(tier.Event.Venue.Capacity * tier.CapacityPercentage)
                    : int.MaxValue;

                tier.AvailableSeats += seatsToRestore;

                // Prevent restoring more than original capacity
                if (tier.AvailableSeats > originalCapacity)
                {
                    tier.AvailableSeats = originalCapacity;
                }

                _context.Set<EventTicketTier>().Update(tier);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}