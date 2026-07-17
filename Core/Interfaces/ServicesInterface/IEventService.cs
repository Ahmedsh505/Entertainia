using Core.DTOs;
using Core.Enums;
using Core.Models;
using Core.Models.EventModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service interface for Event business logic
    /// </summary>
    public interface IEventService
    {
        // ===== EVENT CRUD =====
        Task<EventBase?> GetEventByIdAsync(int eventId);
        Task<EventBase?> GetEventByIdNoTrackingAsync(int eventId);
        Task<List<EventBase>> GetAllEventsAsync();
        Task<List<EventBase>> GetApprovedEventsAsync();
        Task<List<EventBase>> GetPendingEventsAsync();
        Task<List<EventBase>> GetEventsByOrganizerAsync(int organizerId);
        Task<List<EventBase>> GetEventsByCategoryAsync(string category);
        Task<List<EventBase>> GetTrendingEventsAsync(int count = 5);
        Task<List<(DateTime StartDate, DateTime EndDate)>> GetVenueAvailabilityAsync(int venueId);

        // ===== EVENT CREATION & UPDATES =====
        Task<(bool Success, string Message, int? EventId)> CreateEventAsync(
            EventBase eventBase,
            int currentUserId,
            bool isAdmin,
            bool autoCreateTiers = true);

        Task<(bool Success, string Message)> UpdateEventAsync(
            EventBase eventBase,
            int currentUserId,
            bool isAdmin);

        Task<(bool Success, string Message)> DeleteEventAsync(int eventId, int currentUserId, bool isAdmin);

        // ===== VALIDATION =====
        Task<(bool IsValid, string ErrorMessage)> ValidateEventAsync(EventBase eventBase, int? excludeEventId = null);
        bool ValidateEventDates(DateTime startDate, DateTime endDate, out string errorMessage);
        Task<bool> HasVenueScheduleConflictAsync(int venueId, DateTime startDate, DateTime endDate, int? excludeEventId = null);

        // ===== APPROVAL WORKFLOW =====
        Task<(bool Success, string Message)> ApproveEventAsync(int eventId, int adminUserId, string? note = null);
        Task<(bool Success, string Message)> RejectEventAsync(int eventId, int adminUserId, string? note = null);
        Task<(bool Success, string Message)> SetApprovalStatusAsync(
            int eventId,
            ApprovalStatus status,
            int adminUserId,
            string? note = null);

        // ===== TICKET TIERS =====
        Task<List<EventTicketTier>> GetEventTicketTiersAsync(int eventId);
        Task<(bool Success, string Message)> CreateDefaultTicketTiersAsync(int eventId, int venueCapacity);

        // ===== DISCOUNT CODES =====
        Task<List<DiscountCode>> GetEventDiscountCodesAsync(int eventId);
        Task<(bool Success, string Message)> CreateDiscountCodeAsync(DiscountCode discountCode, int createdByUserId);
        Task<(bool Success, string Message)> UpdateDiscountCodesAsync(int eventId, List<DiscountCodeDTO> discountDTOs, int updatedByUserId);

        // ===== AVAILABILITY =====
        Task<int> GetAvailableSeatsAsync(int eventId, int? excludeBookingId = null);
        Task<bool> IsEventSoldOutAsync(int eventId);

        // ===== SUB-EVENT MANAGEMENT =====
        Task<(bool Success, string Message)> CreateSubEventAsync(int eventId, string category, object? subEventData);
        Task<(bool Success, string Message)> UpdateSubEventAsync(int eventId, string category, object? subEventData);

        Task<(List<EventBase> Events, int TotalCount)> GetEventsForUserAsync(
          string? role,
          int? userId = null,
          string? searchTerm = null,
          string? sortColumn = null,
          string? sortDirection = "asc",
          int page = 1,
          int pageSize = 5);


    }
}