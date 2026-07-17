using Core.Enums;
using Core.Models.EventModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IEventTicketTierRepo
    {
        void Add(EventTicketTier tier);
        void Update(EventTicketTier tier);
        void Delete(EventTicketTier tier);
        List<EventTicketTier> GetAll();
        EventTicketTier? GetById(int id);
        List<EventTicketTier> GetByEvent(int eventId);
        EventTicketTier? GetByEventAndTier(int eventId, TicketTier tier);
        void UpdateAvailableSeats(int tierId, int seatsToReduce);

        // ✅ NEW: Restore seats method
        void RestoreAvailableSeats(int tierId, int seatsToRestore);

        void Save();
    }
}