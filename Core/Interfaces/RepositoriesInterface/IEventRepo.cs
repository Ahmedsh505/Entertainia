using Core.Enums;
using Core.Models.EventModels;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IEventRepo
    {
        // Basic CRUD
        void Add(EventBase ev);
        void Update(EventBase ev);
        void Delete(EventBase ev);
        List<EventBase> GetAll();
        EventBase GetById(int id);
        EventBase GetByIdNoTracking(int id);
        void Detach(EventBase entity);
        List<EventBase> GetByCategory(string category);
        Task<List<EventBase>> GetTrendingOrUpcomingEventsAsync(int count = 5);
        List<EventBase> GetByOrganizer(int organizerId);
        Task<List<EventBase>> GetBookedEventsByVenueAsync(int venueId);
        void Save();

        // --- New methods for approval workflow ---

        /// <summary>
        /// Returns all events awaiting admin approval.
        /// </summary>
        List<EventBase> GetPendingEvents();

        /// <summary>
        /// Returns all approved & published events (publicly visible).
        /// </summary>
        List<EventBase> GetApprovedEvents();

        /// <summary>
        /// Updates the approval status for a given event.
        /// </summary>
        void SetApprovalStatus(int eventId, ApprovalStatus status, int? approvedById = null, string? note = null);
        Task<List<EventBase>> GetAllEventsAsync();
        Task<List<EventBase>> GetEventsByOrganizerAsync(int organizerId);
        Task<List<EventBase>> GetApprovedEventsAsync();







    }





}
