using Core.DTOs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.EventModels;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Service.Services
{
    /// <summary>
    /// Service for Event business logic and operations
    /// </summary>
    public class EventService : IEventService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<EventService> _logger;
        private readonly IEgyptTimeService _egyptTime;

        public EventService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<EventService> logger,
            IEgyptTimeService egyptTime)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _egyptTime = egyptTime;
        }



        #region Event CRUD Operations

        public async Task<EventBase?> GetEventByIdAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.Events.GetById(eventId));
        }

        public async Task<EventBase?> GetEventByIdNoTrackingAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.Events.GetByIdNoTracking(eventId));
        }

        public async Task<List<EventBase>> GetAllEventsAsync()
        {
            return await Task.Run(() => _unitOfWork.Events.GetAll());
        }

        public async Task<List<EventBase>> GetApprovedEventsAsync()
        {
            return await Task.Run(() => _unitOfWork.Events.GetApprovedEvents());
        }

        public async Task<List<EventBase>> GetPendingEventsAsync()
        {
            return await Task.Run(() => _unitOfWork.Events.GetPendingEvents());
        }

        public async Task<List<EventBase>> GetEventsByOrganizerAsync(int organizerId)
        {
            return await Task.Run(() => _unitOfWork.Events.GetByOrganizer(organizerId));
        }

        public async Task<List<EventBase>> GetEventsByCategoryAsync(string category)
        {
            return await Task.Run(() => _unitOfWork.Events.GetByCategory(category));
        }

        public async Task<List<EventBase>> GetTrendingEventsAsync(int count = 5)
        {
            return await _unitOfWork.Events.GetTrendingOrUpcomingEventsAsync(count);
        }

        #endregion

        #region Create Event

        public async Task<(bool Success, string Message, int? EventId)> CreateEventAsync(
    EventBase eventBase,
    int currentUserId,
    bool isAdmin,
    bool autoCreateTiers = true)
        {
            try
            {
                _logger.LogInformation($"Creating event: {eventBase.Title} by user {currentUserId}");

                // Step 1: Validate event data
                var validation = await ValidateEventAsync(eventBase);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Event validation failed: {validation.ErrorMessage}");
                    return (false, validation.ErrorMessage, null);
                }

                // Step 2: Set organizer and approval status based on role
                if (isAdmin)
                {
                    // Admin creates event for system organizer
                    var systemOrganizer = await GetOrCreateSystemOrganizerAsync();
                    if (systemOrganizer == null)
                    {
                        return (false, "Failed to create system organizer", null);
                    }
                    eventBase.OrganizerId = systemOrganizer.Id;
                    eventBase.ApprovalStatus = ApprovalStatus.Approved;
                    eventBase.ApprovedByUserId = currentUserId;
                    eventBase.ApprovedAt = DateTime.UtcNow;
                }
                else
                {
                    // ✅ NEW: Check if organizer is deleted/deactivated
                    var organizer = await Task.Run(() => _unitOfWork.Users.GetById(currentUserId));
                    if (organizer == null)
                    {
                        _logger.LogWarning($"Organizer not found: {currentUserId}");
                        return (false, "Organizer not found", null);
                    }

                    if (organizer.IsDeleted)
                    {
                        _logger.LogWarning($"Deleted organizer attempted to create event: {currentUserId}");
                        return (false,
                            "Your account is deactivated. You cannot create new events. " +
                            "Contact an administrator to reactivate your account.",
                            null);
                    }

                    if (organizer.AccountStatus != AccountStatus.Active)
                    {
                        _logger.LogWarning($"Inactive organizer attempted to create event: {currentUserId} - Status: {organizer.AccountStatus}");
                        return (false,
                            $"Your account status is {organizer.AccountStatus}. " +
                            "Only active accounts can create events.",
                            null);
                    }

                    // Organizer creates event (needs approval)
                    eventBase.OrganizerId = currentUserId;
                    eventBase.ApprovalStatus = ApprovalStatus.Pending;
                    eventBase.ApprovedByUserId = null;
                    eventBase.ApprovedAt = null;
                }

                // Step 3: Add event to database
                _unitOfWork.Events.Add(eventBase);
                _unitOfWork.Complete();
                var eventId = eventBase.EventId;
                _logger.LogInformation($"Event created successfully with ID: {eventId}");

                // Step 4: Create default ticket tiers if requested
                if (autoCreateTiers)
                {
                    var venue = _unitOfWork.Venues.GetById(eventBase.VenueId);
                    if (venue != null)
                    {
                        await CreateDefaultTicketTiersAsync(eventId, venue.Capacity);
                    }
                }

                return (true, "Event created successfully", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating event: {eventBase.Title}");
                return (false, $"An error occurred while creating the event: {ex.Message}", null);
            }
        }

        #endregion

        #region Update Event
        public async Task<(bool Success, string Message)> UpdateEventAsync(
            EventBase eventBase,
            int currentUserId,
            bool isAdmin)
        {
            try
            {
                _logger.LogInformation($"Updating event ID: {eventBase.EventId} by user {currentUserId}");

                // Step 1: Get existing event (NO TRACKING to avoid conflicts)
                var existingEvent = await GetEventByIdNoTrackingAsync(eventBase.EventId);
                if (existingEvent == null)
                {
                    return (false, "Event not found");
                }

                // Step 2: Authorization check
                if (!isAdmin && existingEvent.OrganizerId != currentUserId)
                {
                    return (false, "You don't have permission to update this event");
                }

                // Step 3: Validate event data (exclude current event from venue conflict check)
                var validation = await ValidateEventAsync(eventBase, eventBase.EventId);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Event validation failed: {validation.ErrorMessage}");
                    return (false, validation.ErrorMessage);
                }

                // ✅ CRITICAL FIX: Detach any tracked entities with the same ID
                var trackedEntity = _unitOfWork.Events.GetById(eventBase.EventId);
                if (trackedEntity != null)
                {
                    _unitOfWork.Events.Detach(trackedEntity);
                }

                // Step 4: Preserve important fields from existing event
                eventBase.CreatedAt = existingEvent.CreatedAt; // Preserve creation date

                // Step 5: Handle approval status
                if (isAdmin)
                {
                    // Admin can keep or change approval status
                    // If not explicitly set, maintain current approval
                    if (eventBase.ApprovalStatus == ApprovalStatus.Pending &&
                        existingEvent.ApprovalStatus == ApprovalStatus.Approved)
                    {
                        eventBase.ApprovalStatus = ApprovalStatus.Approved;
                        eventBase.ApprovedByUserId = existingEvent.ApprovedByUserId;
                        eventBase.ApprovedAt = existingEvent.ApprovedAt;
                    }
                    else if (eventBase.ApprovalStatus == ApprovalStatus.Approved &&
                             existingEvent.ApprovalStatus != ApprovalStatus.Approved)
                    {
                        // Admin is approving now
                        eventBase.ApprovedByUserId = currentUserId;
                        eventBase.ApprovedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Organizer edit - preserve organizer ID and reset approval
                    eventBase.OrganizerId = existingEvent.OrganizerId;
                    eventBase.ApprovalStatus = ApprovalStatus.Pending;
                    eventBase.ApprovedByUserId = null;
                    eventBase.ApprovedAt = null;
                    eventBase.ApprovalNote = null;
                }

                // Step 6: Update event
                _unitOfWork.Events.Update(eventBase);
                _unitOfWork.Complete();

                _logger.LogInformation($"Event ID: {eventBase.EventId} updated successfully");
                return (true, "Event updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating event ID: {eventBase.EventId}");
                return (false, $"An error occurred while updating the event: {ex.Message}");
            }
        }

        #endregion

        #region Delete Event

        public async Task<(bool Success, string Message)> DeleteEventAsync(
            int eventId,
            int currentUserId,
            bool isAdmin)
        {
            try
            {
                _logger.LogInformation($"Deleting event ID: {eventId} by user {currentUserId}");

                // Step 1: Get event
                var eventBase = await GetEventByIdAsync(eventId);
                if (eventBase == null)
                {
                    return (false, "Event not found");
                }

                // Step 2: Authorization check
                if (!isAdmin && eventBase.OrganizerId != currentUserId)
                {
                    return (false, "You don't have permission to delete this event");
                }

                // Step 3: Check if event has bookings
                var hasBookings = eventBase.Bookings?.Any(b => b.Status != "Cancelled") ?? false;
                if (hasBookings)
                {
                    return (false, "Cannot delete event with active bookings. Please cancel all bookings first.");
                }

                // Step 4: Delete event (cascade will handle sub-events)
                _unitOfWork.Events.Delete(eventBase);
                _unitOfWork.Complete();

                _logger.LogInformation($"Event ID: {eventId} deleted successfully");
                return (true, "Event deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting event ID: {eventId}");
                return (false, $"An error occurred while deleting the event: {ex.Message}");
            }
        }

        #endregion

        #region Sub-Event Management

        public async Task<(bool Success, string Message)> CreateSubEventAsync(
            int eventId,
            string category,
            object? subEventData)
        {
            try
            {
                if (subEventData == null)
                {
                    return (true, "No sub-event data provided");
                }

                switch (category)
                {
                    case "Concert":
                        var concert = subEventData as Concert;
                        if (concert != null)
                        {
                            concert.ConcertId = eventId;
                            _unitOfWork.Concerts.Add(concert);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "SportEvent":
                        var sportEvent = subEventData as SportEvent;
                        if (sportEvent != null)
                        {
                            sportEvent.SportId = eventId;
                            _unitOfWork.SportEvents.Add(sportEvent);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "TheaterPlay":
                        var theaterPlay = subEventData as TheaterPlay;
                        if (theaterPlay != null)
                        {
                            theaterPlay.TheaterId = eventId;
                            _unitOfWork.TheaterPlays.Add(theaterPlay);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "CulturalEvent":
                        var culturalEvent = subEventData as CulturalEvent;
                        if (culturalEvent != null)
                        {
                            culturalEvent.CulturalId = eventId;
                            _unitOfWork.CulturalEvents.Add(culturalEvent);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "TourismActivity":
                        var tourismActivity = subEventData as TourismActivity;
                        if (tourismActivity != null)
                        {
                            tourismActivity.TourismId = eventId;
                            _unitOfWork.TourismActivities.Add(tourismActivity);
                            _unitOfWork.Complete();
                        }
                        break;

                    default:
                        return (false, $"Unknown event category: {category}");
                }

                return (true, "Sub-event created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating sub-event for event ID: {eventId}");
                return (false, $"Error creating sub-event: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateSubEventAsync(
            int eventId,
            string category,
            object? subEventData)
        {
            try
            {
                if (subEventData == null)
                {
                    return (true, "No sub-event data provided");
                }

                switch (category)
                {
                    case "Concert":
                        var concert = subEventData as Concert;
                        if (concert != null)
                        {
                            concert.ConcertId = eventId;
                            var existing = _unitOfWork.Concerts.GetById(eventId);
                            if (existing != null)
                                _unitOfWork.Concerts.Update(concert);
                            else
                                _unitOfWork.Concerts.Add(concert);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "SportEvent":
                        var sportEvent = subEventData as SportEvent;
                        if (sportEvent != null)
                        {
                            sportEvent.SportId = eventId;
                            var existing = _unitOfWork.SportEvents.GetById(eventId);
                            if (existing != null)
                                _unitOfWork.SportEvents.Update(sportEvent);
                            else
                                _unitOfWork.SportEvents.Add(sportEvent);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "TheaterPlay":
                        var theaterPlay = subEventData as TheaterPlay;
                        if (theaterPlay != null)
                        {
                            theaterPlay.TheaterId = eventId;
                            var existing = _unitOfWork.TheaterPlays.GetById(eventId);
                            if (existing != null)
                                _unitOfWork.TheaterPlays.Update(theaterPlay);
                            else
                                _unitOfWork.TheaterPlays.Add(theaterPlay);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "CulturalEvent":
                        var culturalEvent = subEventData as CulturalEvent;
                        if (culturalEvent != null)
                        {
                            culturalEvent.CulturalId = eventId;
                            var existing = _unitOfWork.CulturalEvents.GetById(eventId);
                            if (existing != null)
                                _unitOfWork.CulturalEvents.Update(culturalEvent);
                            else
                                _unitOfWork.CulturalEvents.Add(culturalEvent);
                            _unitOfWork.Complete();
                        }
                        break;

                    case "TourismActivity":
                        var tourismActivity = subEventData as TourismActivity;
                        if (tourismActivity != null)
                        {
                            tourismActivity.TourismId = eventId;
                            var existing = _unitOfWork.TourismActivities.GetById(eventId);
                            if (existing != null)
                                _unitOfWork.TourismActivities.Update(tourismActivity);
                            else
                                _unitOfWork.TourismActivities.Add(tourismActivity);
                            _unitOfWork.Complete();
                        }
                        break;

                    default:
                        return (false, $"Unknown event category: {category}");
                }

                return (true, "Sub-event updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating sub-event for event ID: {eventId}");
                return (false, $"Error updating sub-event: {ex.Message}");
            }
        }

        #endregion

        #region Validation

        public async Task<(bool IsValid, string ErrorMessage)> ValidateEventAsync(
    EventBase eventBase,
    int? excludeEventId = null)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(eventBase.Title))
            {
                return (false, "Event title is required");
            }
            if (eventBase.VenueId <= 0)
            {
                return (false, "Please select a valid venue");
            }
            if (eventBase.Price <= 0)
            {
                return (false, "Price must be greater than 0");
            }

            // Validate dates
            if (!ValidateEventDates(eventBase.StartDate, eventBase.EndDate, out string dateError))
            {
                return (false, dateError);
            }

            // ✅ Check venue exists (including deleted venues)
            var venue = await Task.Run(() => _unitOfWork.Venues.GetById(eventBase.VenueId));

            if (venue == null)
            {
                return (false, "Selected venue does not exist");
            }

            // ✅ NEW: Check if venue is deleted
            if (venue.IsDeleted)
            {
                return (false,
                    "This venue is no longer active and cannot be used for new events. " +
                    "Please select a different venue.");
            }

            if (venue.ApprovalStatus != ApprovalStatus.Approved)
            {
                return (false, "Selected venue is not approved");
            }

            // Check for venue scheduling conflicts
            if (await HasVenueScheduleConflictAsync(
                eventBase.VenueId,
                eventBase.StartDate,
                eventBase.EndDate,
                excludeEventId))
            {
                return (false, "This venue is already booked for another event during the selected time period");
            }

            return (true, string.Empty);
        }

        public bool ValidateEventDates(DateTime startDate, DateTime endDate, out string errorMessage)
        {
            errorMessage = string.Empty;

            // ✅ FIX: Allow events that start within the last 10 minutes to account for form submission time
            // This prevents validation failures when the user takes time to fill out the form
            var minimumStartDate = DateTime.Now.AddMinutes(-10);

            if (startDate < minimumStartDate)
            {
                errorMessage = "Start date cannot be more than 10 minutes in the past";
                return false;
            }

            // Check if end date is after start date
            if (endDate <= startDate)
            {
                errorMessage = "End date must be after start date";
                return false;
            }

            // Check minimum duration (30 minutes)
            var duration = endDate - startDate;
            if (duration.TotalMinutes < 30)
            {
                errorMessage = "Event duration must be at least 30 minutes";
                return false;
            }

            return true;
        }

        public async Task<bool> HasVenueScheduleConflictAsync(
            int venueId,
            DateTime startDate,
            DateTime endDate,
            int? excludeEventId = null)
        {
            return await Task.Run(() =>
            {
                var venue = _unitOfWork.Venues.GetByIdWithEvents(venueId);
                if (venue == null) return false;

                return venue.HasScheduleConflict(startDate, endDate, excludeEventId);
            });
        }

        #endregion

        #region Approval Workflow

        public async Task<(bool Success, string Message)> ApproveEventAsync(
            int eventId,
            int adminUserId,
            string? note = null)
        {
            return await SetApprovalStatusAsync(eventId, ApprovalStatus.Approved, adminUserId, note);
        }

        public async Task<(bool Success, string Message)> RejectEventAsync(
            int eventId,
            int adminUserId,
            string? note = null)
        {
            return await SetApprovalStatusAsync(eventId, ApprovalStatus.Rejected, adminUserId, note);
        }

        public async Task<(bool Success, string Message)> SetApprovalStatusAsync(
            int eventId,
            ApprovalStatus status,
            int adminUserId,
            string? note = null)
        {
            try
            {
                _logger.LogInformation($"Setting approval status for event ID: {eventId} to {status}");

                // Get event with organizer info
                var eventBase = await GetEventByIdAsync(eventId);
                if (eventBase == null)
                {
                    return (false, "Event not found");
                }

                // Update approval status
                _unitOfWork.Events.SetApprovalStatus(eventId, status, adminUserId, note);

                // Send email notification to organizer
                if (eventBase.Organizer != null && !string.IsNullOrEmpty(eventBase.Organizer.Email))
                {
                    try
                    {
                        if (status == ApprovalStatus.Approved)
                        {
                            await _emailService.SendEventApprovedAsync(
                                eventBase.Organizer.Email,
                                $"{eventBase.Organizer.FirstName} {eventBase.Organizer.LastName}",
                                eventBase.Title ?? "Your Event"
                            );
                        }
                        else if (status == ApprovalStatus.Rejected)
                        {
                            await _emailService.SendEventRejectedAsync(
                                eventBase.Organizer.Email,
                                $"{eventBase.Organizer.FirstName} {eventBase.Organizer.LastName}",
                                eventBase.Title ?? "Your Event",
                                note
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send approval email for event ID: {eventId}");
                        // Don't fail the operation if email fails
                    }
                }

                var statusMessage = status == ApprovalStatus.Approved ? "approved" : "rejected";
                _logger.LogInformation($"Event ID: {eventId} {statusMessage} successfully");

                return (true, $"Event {statusMessage} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting approval status for event ID: {eventId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Ticket Tiers

        public async Task<List<EventTicketTier>> GetEventTicketTiersAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.TicketTiers.GetByEvent(eventId));
        }

        public async Task<(bool Success, string Message)> CreateDefaultTicketTiersAsync(
            int eventId,
            int venueCapacity)
        {
            try
            {
                _logger.LogInformation($"Creating default ticket tiers for event ID: {eventId}");

                var tiers = EventTicketTier.CreateDefaultTiers(eventId, venueCapacity);

                foreach (var tier in tiers)
                {
                    _unitOfWork.TicketTiers.Add(tier);
                }

                _unitOfWork.Complete();

                _logger.LogInformation($"Created {tiers.Count} ticket tiers for event ID: {eventId}");
                return (true, "Ticket tiers created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating ticket tiers for event ID: {eventId}");
                return (false, $"Error creating ticket tiers: {ex.Message}");
            }
        }

        #endregion

        #region Discount Codes

        public async Task<List<DiscountCode>> GetEventDiscountCodesAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.DiscountCodes.GetByEvent(eventId));
        }

        public async Task<(bool Success, string Message)> CreateDiscountCodeAsync(
        DiscountCode discountCode,
        int createdByUserId)
        {
            try
            {
                // ✅ Validation
                if (string.IsNullOrWhiteSpace(discountCode.Code))
                    return (false, "Discount code is required");

                if (discountCode.DiscountValue <= 0)
                    return (false, "Discount value must be greater than 0");

                // ✅ Ensure unique per event
                var existingCode = _unitOfWork.DiscountCodes.GetValidCodeForEvent(
                    discountCode.Code,
                    discountCode.EventId);

                if (existingCode != null)
                    return (false, $"Discount code '{discountCode.Code}' already exists for this event");

                // ✅ Set metadata

                discountCode.CreatedByUserId = createdByUserId;

                // Always store as UTC (converted from Egypt time)
                discountCode.CreatedAt = TimeHelper.ToUtcFromEgypt(TimeHelper.NowEgypt);

                // If ValidFrom is provided, convert to UTC; otherwise, default = now - 2h (Egypt time)
                discountCode.ValidFrom = TimeHelper.ToUtcFromEgypt(
                    discountCode.ValidFrom ?? TimeHelper.NowEgypt
                );

                // Convert ValidUntil if present
                discountCode.ValidUntil = TimeHelper.ToUtcFromEgypt(discountCode.ValidUntil);

                discountCode.CurrentUsageCount = 0;

                discountCode.CurrentUsageCount = 0;

                if (discountCode.DiscountType == DiscountType.Percentage &&
                (discountCode.DiscountValue <= 0 || discountCode.DiscountValue > 100))
                {
                    return (false, "Percentage discount must be between 0.01 and 100.");
                }

                if (discountCode.DiscountType == DiscountType.FixedAmount && discountCode.DiscountValue <= 0)
                {
                    return (false, "Fixed amount discount must be greater than 0.");
                }

                // ⚡ Just add — save will be done once in controller
                await _unitOfWork.DiscountCodes.AddAsync(discountCode);

                _logger.LogInformation($"[Pending Save] Discount code '{discountCode.Code}' added to event {discountCode.EventId}");
                return (true, "Discount code prepared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating discount code: {discountCode.Code}");
                return (false, $"Error creating discount code: {ex.Message}");
            }
        }

        #endregion

        #region Update Discount Codes

        public async Task<(bool Success, string Message)> UpdateDiscountCodesAsync(int eventId, List<DiscountCodeDTO> discountDTOs, int updatedByUserId)
        {
            try
            {
                var existingCodes = _unitOfWork.DiscountCodes.GetByEvent(eventId);

                // Track IDs sent from form
                var sentIds = discountDTOs.Where(d => d.DiscountCodeId > 0).Select(d => d.DiscountCodeId).ToList();

                // 1️⃣ حذف الأكواد اللي اتحذفت من الواجهة
                var removed = existingCodes.Where(d => !sentIds.Contains(d.DiscountCodeId)).ToList();
                foreach (var code in removed)
                    _unitOfWork.DiscountCodes.Delete(code);

                // 2️⃣ تحديث أو إضافة الأكواد الجديدة
                foreach (var vm in discountDTOs)
                {
                    if (vm.DiscountCodeId == 0)
                    {
                        // جديد
                        var newCode = new DiscountCode
                        {
                            EventId = eventId,
                            Code = vm.Code,
                            DiscountType = vm.DiscountType,
                            DiscountValue = vm.DiscountValue,
                            ValidFrom = TimeHelper.ToUtcFromEgypt(vm.ValidFrom ?? TimeHelper.NowEgypt),
                            ValidUntil = TimeHelper.ToUtcFromEgypt(vm.ValidUntil),
                            MaxUsageCount = vm.MaxUsageCount,
                            IsActive = vm.IsActive,
                            CreatedByUserId = updatedByUserId,
                            CreatedAt = TimeHelper.ToUtcFromEgypt(TimeHelper.NowEgypt)
                        };
                        _unitOfWork.DiscountCodes.Add(newCode);
                    }
                    else
                    {
                        // تعديل
                        var existing = existingCodes.FirstOrDefault(d => d.DiscountCodeId == vm.DiscountCodeId);
                        if (existing != null)
                        {
                            existing.Code = vm.Code;
                            existing.DiscountType = vm.DiscountType;
                            existing.DiscountValue = vm.DiscountValue;
                            existing.ValidFrom = TimeHelper.ToUtcFromEgypt(vm.ValidFrom ?? TimeHelper.NowEgypt);
                            existing.ValidUntil = TimeHelper.ToUtcFromEgypt(vm.ValidUntil);
                            existing.MaxUsageCount = vm.MaxUsageCount;
                            existing.IsActive = vm.IsActive;
                            _unitOfWork.DiscountCodes.Update(existing);
                        }
                    }
                }

                await _unitOfWork.SaveAsync();
                return (true, "Discount codes updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount codes for event {EventId}", eventId);
                return (false, $"Error updating discount codes: {ex.Message}");
            }
        }

        #endregion

        #region Availability

        public async Task<int> GetAvailableSeatsAsync(int eventId, int? excludeBookingId = null)
        {
            return await Task.Run(() =>
            {
                var eventEntity = _unitOfWork.Events.GetById(eventId);
                if (eventEntity?.Venue == null)
                    return 0;

                var bookedTickets = _unitOfWork.Bookings
                    .GetAll()
                    .Where(b => b.EventId == eventId
                           && b.Status != "Cancelled"
                           && (!excludeBookingId.HasValue || b.BookingId != excludeBookingId.Value))
                    .Sum(b => (int?)b.NumberOfTickets) ?? 0;

                return eventEntity.Venue.Capacity - bookedTickets;
            });
        }

        public async Task<bool> IsEventSoldOutAsync(int eventId)
        {
            var availableSeats = await GetAvailableSeatsAsync(eventId);
            return availableSeats <= 0;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get or create the system organizer "Entertainia"
        /// </summary>
        private async Task<User?> GetOrCreateSystemOrganizerAsync()
        {
            return await Task.Run(() =>
            {
                // Try to find existing system organizer
                var organizers = _unitOfWork.Users.GetOrganizer();
                var systemOrganizer = organizers.FirstOrDefault(u =>
                    u.Email == "entertainia@system.com" ||
                    u.FirstName == "Entertainia");

                if (systemOrganizer != null)
                {
                    return systemOrganizer;
                }

                // Create new system organizer
                var newOrganizer = new User
                {
                    FirstName = "Entertainia",
                    LastName = "System",
                    Email = "entertainia@system.com",
                    UserName = "entertainia@system.com",
                    Role = "Organizer",
                    AccountStatus = AccountStatus.Active,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Users.Add(newOrganizer);
                _unitOfWork.Complete();

                _logger.LogInformation("System organizer 'Entertainia' created successfully");
                return newOrganizer;
            });
        }
        public async Task<List<(DateTime StartDate, DateTime EndDate)>> GetVenueAvailabilityAsync(int venueId)
        {
            var events = await _unitOfWork.Events.GetBookedEventsByVenueAsync(venueId);

            return events
                .Select(e => (e.StartDate, e.EndDate))
                .OrderBy(e => e.StartDate)
                .ToList();
        }

        #endregion


        public async Task<(List<EventBase> Events, int TotalCount)> GetEventsForUserAsync(
            string? role,
            int? userId = null,
            string? searchTerm = null,
            string? sortColumn = null,
            string? sortDirection = "asc",
            int page = 1,
            int pageSize = 5)
        {
            List<EventBase> eventsList;

            // 🧩 Get events based on role
            if (role == "Admin")
            {
                eventsList = await _unitOfWork.Events.GetAllEventsAsync();
            }
            else if (role == "Organizer" && userId.HasValue)
            {
                eventsList = await _unitOfWork.Events.GetEventsByOrganizerAsync(userId.Value);
            }
            else // Customer
            {
                eventsList = await _unitOfWork.Events.GetApprovedEventsAsync();

                // ✅ Show future events only
                var today = DateTime.Now.Date;
                eventsList = eventsList
                    .Where(e => e.StartDate.Date > today)
                    .ToList();
            }

            // 🔍 Search
            if (!string.IsNullOrEmpty(searchTerm))
            {
                eventsList = eventsList
                    .Where(e => e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 🧭 Sorting
            bool ascending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            eventsList = sortColumn switch
            {
                "Title" => ascending
                    ? eventsList.OrderBy(e => e.Title).ToList()
                    : eventsList.OrderByDescending(e => e.Title).ToList(),

                "StartDate" => ascending
                    ? eventsList.OrderBy(e => e.StartDate).ToList()
                    : eventsList.OrderByDescending(e => e.StartDate).ToList(),

                "Price" => ascending
                    ? eventsList.OrderBy(e => e.Price).ToList()
                    : eventsList.OrderByDescending(e => e.Price).ToList(),

                // ✅ NEW: Governorate sorting
                "Governorate" => ascending
                    ? eventsList.OrderBy(e => e.Venue?.Governorate).ToList()
                    : eventsList.OrderByDescending(e => e.Venue?.Governorate).ToList(),

                // Default: sort by start date
                _ => ascending
                    ? eventsList.OrderBy(e => e.StartDate).ToList()
                    : eventsList.OrderByDescending(e => e.StartDate).ToList(),
            };

            // 📄 Pagination
            int totalCount = eventsList.Count;
            var pagedEvents = eventsList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (pagedEvents, totalCount);
        }


    }
}