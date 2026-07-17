using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.EventModels;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    public class VenueService : IVenueService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VenueService> _logger;
        private readonly IEgyptTimeService _timeService;

        public VenueService(
            IUnitOfWork unitOfWork,
            ILogger<VenueService> logger,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _timeService = timeService;
        }

        #region Venue Listing & Filtering

        // ✅ UPDATED: Added governorate filter
        public async Task<VenueListData> GetVenuesAsync(
            string? searchTerm,
            string? sortOrder,
            int? governorateFilter,  // ✅ NEW parameter
            int page,
            int pageSize,
            int? currentUserId = null,
            bool isAdmin = false)
        {
            try
            {
                // Get venues based on role
                IEnumerable<Venue> venuesQuery;

                if (isAdmin)
                {
                    venuesQuery = _unitOfWork.Venues.GetAll();
                }
                else if (currentUserId.HasValue)
                {
                    venuesQuery = _unitOfWork.Venues.GetQuery()
                        .Where(v => v.ApprovalStatus == ApprovalStatus.Approved ||
                                    v.CreatedByUserId == currentUserId.Value)
                        .ToList();
                }
                else
                {
                    venuesQuery = _unitOfWork.Venues.GetApprovedVenues();
                }

                var query = venuesQuery.AsQueryable();

                // ✅ NEW: Apply governorate filter
                if (governorateFilter.HasValue)
                {
                    var governorateEnum = (Governorate)governorateFilter.Value;
                    query = query.Where(v => v.Governorate == governorateEnum);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    string lowerSearch = searchTerm.ToLower();
                    query = query.Where(v =>
                        (v.Name != null && v.Name.ToLower().Contains(lowerSearch)) ||
                        (v.Location != null && v.Location.ToLower().Contains(lowerSearch)));
                }

                // ✅ UPDATED: Apply sorting (added governorate option)
                query = sortOrder switch
                {
                    "name_desc" => query.OrderByDescending(v => v.Name),
                    "location" => query.OrderBy(v => v.Location),
                    "location_desc" => query.OrderByDescending(v => v.Location),
                    "capacity" => query.OrderBy(v => v.Capacity),
                    "capacity_desc" => query.OrderByDescending(v => v.Capacity),
                    "governorate" => query.OrderBy(v => v.Governorate),  // ✅ NEW
                    "governorate_desc" => query.OrderByDescending(v => v.Governorate),  // ✅ NEW
                    _ => query.OrderBy(v => v.Name)
                };

                // Get total count before pagination
                int totalItems = query.Count();

                // Apply pagination
                var venues = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new VenueListData
                {
                    Venues = venues,
                    SearchTerm = searchTerm,
                    SortOrder = sortOrder,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                _logger.LogInformation($"Retrieved {venues.Count} venues (page {page}/{result.TotalPages})");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting venues list");
                throw;
            }
        }

        #endregion

        #region Venue Details

        public async Task<(bool Success, Venue? Venue, List<EventBase> Events, string ErrorMessage)> GetVenueDetailsAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetByIdWithEvents(venueId);

                if (venue == null)
                    return (false, null, new List<EventBase>(), "Venue not found");

                // Check if user can view this venue
                if (!CanUserViewVenue(venue, currentUserId, isAdmin))
                {
                    return (false, null, new List<EventBase>(),
                        "You do not have permission to view this venue");
                }

                // Filter events based on user permissions
                var venueEvents = venue.Events?
                    .Where(e => e.ApprovalStatus == ApprovalStatus.Approved || isAdmin)
                    .OrderBy(e => e.StartDate)
                    .ToList() ?? new List<EventBase>();

                _logger.LogInformation($"Retrieved details for venue {venueId} with {venueEvents.Count} events");
                return (true, venue, venueEvents, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting venue details for venue {venueId}");
                return (false, null, new List<EventBase>(), $"Error retrieving venue: {ex.Message}");
            }
        }

        #endregion

        #region Venue CRUD

        public async Task<(bool Success, int VenueId, string Message)> CreateVenueAsync(
            VenueCreationData data,
            int? organizerId = null,
            bool isAdmin = false)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(data.Name))
                    return (false, 0, "Venue name is required");

                if (string.IsNullOrWhiteSpace(data.Location))
                    return (false, 0, "Location is required");

                if (data.Capacity <= 0)
                    return (false, 0, "Capacity must be greater than 0");

                // Create venue entity
                var venue = new Venue
                {
                    Name = data.Name,
                    Location = data.Location,
                    Capacity = data.Capacity,
                    Governorate = data.Governorate,  // ✅ NEW: Set governorate
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    GoogleMapsUrl = data.GoogleMapsUrl
                };

                // Set approval status based on role
                if (isAdmin)
                {
                    venue.ApprovalStatus = ApprovalStatus.Approved;
                    venue.ApprovedByUserId = organizerId;
                    venue.ApprovedAt = DateTime.UtcNow;
                    venue.CreatedByUserId = organizerId;
                }
                else
                {
                    venue.ApprovalStatus = ApprovalStatus.Pending;
                    venue.ApprovedByUserId = null;
                    venue.ApprovedAt = null;
                    venue.CreatedByUserId = organizerId;
                }

                // Generate Google Maps URL if coordinates provided
                if (data.Latitude.HasValue && data.Longitude.HasValue && string.IsNullOrEmpty(data.GoogleMapsUrl))
                {
                    venue.GoogleMapsUrl = $"https://www.openstreetmap.org/?mlat={data.Latitude.Value}&mlon={data.Longitude.Value}#map=18/{data.Latitude.Value}/{data.Longitude.Value}";
                }

                _unitOfWork.Venues.Add(venue);
                _unitOfWork.Venues.Save();

                _logger.LogInformation($"Venue {venue.VenueId} created successfully by {(isAdmin ? "admin" : "organizer")} {organizerId}");
                return (true, venue.VenueId, "Venue created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue");
                return (false, 0, $"Error creating venue: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateVenueAsync(
            int venueId,
            VenueCreationData data,
            int? currentUserId = null,
            bool isAdmin = false)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetById(venueId);
                if (venue == null)
                    return (false, "Venue not found");

                // Check permissions
                if (!CanUserEditVenue(venue, currentUserId ?? 0, isAdmin))
                    return (false, "You do not have permission to edit this venue");

                // Validate
                if (string.IsNullOrWhiteSpace(data.Name))
                    return (false, "Venue name is required");

                if (string.IsNullOrWhiteSpace(data.Location))
                    return (false, "Location is required");

                if (data.Capacity <= 0)
                    return (false, "Capacity must be greater than 0");

                // Update venue properties
                venue.Name = data.Name;
                venue.Location = data.Location;
                venue.Capacity = data.Capacity;
                venue.Governorate = data.Governorate;  // ✅ NEW: Update governorate
                venue.Latitude = data.Latitude;
                venue.Longitude = data.Longitude;
                venue.GoogleMapsUrl = data.GoogleMapsUrl;

                // Generate Google Maps URL if coordinates provided
                if (data.Latitude.HasValue && data.Longitude.HasValue && string.IsNullOrEmpty(data.GoogleMapsUrl))
                {
                    venue.GoogleMapsUrl = $"https://www.openstreetmap.org/?mlat={data.Latitude.Value}&mlon={data.Longitude.Value}#map=18/{data.Latitude.Value}/{data.Longitude.Value}";
                }

                // Handle approval status based on role
                if (isAdmin)
                {
                    // Admin keeps approval status as is
                }
                else
                {
                    // Organizer edit - reset approval status
                    venue.ApprovalStatus = ApprovalStatus.Pending;
                    venue.ApprovedByUserId = null;
                    venue.ApprovedAt = null;
                }

                _unitOfWork.Venues.Update(venue);
                _unitOfWork.Venues.Save();

                _logger.LogInformation($"Venue {venueId} updated successfully by {(isAdmin ? "admin" : "organizer")} {currentUserId}");
                return (true, "Venue updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating venue {venueId}");
                return (false, $"Error updating venue: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteVenueAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetById(venueId);
                if (venue == null)
                    return (false, "Venue not found");

                // Check permissions
                if (!CanUserEditVenue(venue, currentUserId ?? 0, isAdmin))
                    return (false, "You do not have permission to delete this venue");

                _unitOfWork.Venues.Delete(venue);
                _unitOfWork.Venues.Save();

                _logger.LogInformation($"Venue {venueId} deleted successfully by user {currentUserId}");
                return (true, "Venue deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting venue {venueId}");
                return (false, $"Error deleting venue: {ex.Message}");
            }
        }

        #endregion

        #region Venue Validation

        public bool CanUserEditVenue(Venue venue, int currentUserId, bool isAdmin)
        {
            if (isAdmin)
                return true;

            return venue.CreatedByUserId.HasValue && venue.CreatedByUserId.Value == currentUserId;
        }

        public bool CanUserViewVenue(Venue venue, int? currentUserId, bool isAdmin)
        {
            if (isAdmin)
                return true;

            if (venue.IsPublished)
                return true;

            if (currentUserId.HasValue && venue.CreatedByUserId.HasValue)
                return venue.CreatedByUserId.Value == currentUserId.Value;

            return false;
        }

        #endregion

        #region Venue Retrieval

        public async Task<(bool Success, Venue? Venue, string ErrorMessage)> GetVenueForEditAsync(
            int venueId,
            int? currentUserId = null,
            bool isAdmin = false)
        {
            try
            {
                var venue = _unitOfWork.Venues.GetById(venueId);
                if (venue == null)
                    return (false, null, "Venue not found");

                // Check permissions
                if (!CanUserEditVenue(venue, currentUserId ?? 0, isAdmin))
                {
                    return (false, null, "You do not have permission to edit this venue");
                }

                return (true, venue, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting venue for edit: {venueId}");
                return (false, null, $"Error retrieving venue: {ex.Message}");
            }
        }

        #endregion
    }
}