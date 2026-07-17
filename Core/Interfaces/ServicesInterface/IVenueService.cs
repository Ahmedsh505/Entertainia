using Core.Enums;
using Core.Models;
using Core.Models.EventModels;

namespace Core.Interfaces.ServicesInterface
{
    public interface IVenueService
    {
        // ===== VENUE LISTING & FILTERING =====
        /// <summary>
        /// Get filtered and paginated venues
        /// </summary>
        Task<VenueListData> GetVenuesAsync(
            string? searchTerm,
            string? sortOrder,
            int? governorateFilter,  // ✅ NEW: Governorate filter
            int page,
            int pageSize,
            int? currentUserId = null,
            bool isAdmin = false);

        // ===== VENUE DETAILS =====
        Task<(bool Success, Venue? Venue, List<EventBase> Events, string ErrorMessage)> GetVenueDetailsAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false);

        // ===== VENUE CRUD =====
        Task<(bool Success, int VenueId, string Message)> CreateVenueAsync(
            VenueCreationData data,
            int? organizerId = null,
            bool isAdmin = false);

        Task<(bool Success, string Message)> UpdateVenueAsync(
            int venueId,
            VenueCreationData data,
            int? currentUserId = null,
            bool isAdmin = false);

        Task<(bool Success, string Message)> DeleteVenueAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false);

        // ===== VENUE VALIDATION =====
        bool CanUserEditVenue(Venue venue, int currentUserId, bool isAdmin);
        bool CanUserViewVenue(Venue venue, int? currentUserId, bool isAdmin);

        // ===== VENUE RETRIEVAL =====
        Task<(bool Success, Venue? Venue, string ErrorMessage)> GetVenueForEditAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false);
    }

    /// <summary>
    /// Domain model for venue list results
    /// </summary>
    public class VenueListData
    {
        public List<Venue> Venues { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortOrder { get; set; }
    }

    /// <summary>
    /// Domain model for venue creation/update
    /// </summary>
    public class VenueCreationData
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public Governorate Governorate { get; set; }  // ✅ NEW: Governorate field
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? GoogleMapsUrl { get; set; }
    }
}