using Core.Enums;
using Core.Models;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IVenueRepo
    {
        // Basic CRUD
        void Add(Venue venue);
        void Update(Venue venue);
        void Delete(Venue venue); // ✅ Now handles soft delete automatically
        List<Venue> GetAll(); // ✅ Returns all including deleted (for admin)
        IQueryable<Venue> GetQuery(); // ✅ Returns only non-deleted (query filter applied)
        Venue GetById(int id); // ✅ Returns venue including deleted
        Venue GetByIdWithEvents(int id); // ✅ Returns venue with events including deleted
        void Save();

        // --- Approval workflow methods ---

        /// <summary>
        /// Returns all non-deleted venues awaiting admin approval.
        /// </summary>
        List<Venue> GetPendingVenues();

        /// <summary>
        /// Returns all venues created by specific organizer (including deleted).
        /// </summary>
        List<Venue> GetByUser(int organizerId);

        /// <summary>
        /// Returns all approved & published venues (non-deleted only).
        /// </summary>
        List<Venue> GetApprovedVenues();
        List<Venue> GetByGovernorate(Governorate governorate);

        /// <summary>
        /// Updates the approval status for a venue.
        /// </summary>
        void SetApprovalStatus(int venueId, ApprovalStatus status, int? approvedById = null, string? note = null);

        // --- ✅ NEW: Soft delete methods ---

        /// <summary>
        /// Soft deletes a venue (marks as deleted, preserves data for events/bookings).
        /// Throws exception if venue has future approved events.
        /// </summary>
        void SoftDeleteVenue(int venueId, int deletedByUserId);

        /// <summary>
        /// Get only active (non-deleted, approved) venues for public listings.
        /// </summary>
        List<Venue> GetActiveVenues();

        /// <summary>
        /// Get venue by ID including deleted venues (for event/booking display).
        /// </summary>
        Venue GetByIdIncludingDeleted(int id);
    }
}