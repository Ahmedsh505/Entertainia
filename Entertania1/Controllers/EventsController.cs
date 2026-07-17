using Core.DTOs;
using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.EventModels;
using Entertania1.ViewModels.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    public class EventsController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IPhotoGalleryService _photoService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            IEventService eventService,
            IPhotoGalleryService photoService,
            IUnitOfWork unitOfWork,
            ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _photoService = photoService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out var currentUserId);
            return currentUserId;
        }

        private List<EventBase> ApplySorting(IEnumerable<EventBase> events, string? sortColumn, string? sortDirection)
        {
            if (string.IsNullOrEmpty(sortColumn))
                return events.ToList();

            bool ascending = sortDirection?.ToLower() != "desc";

            return sortColumn switch
            {
                "Title" => ascending ? events.OrderBy(e => e.Title).ToList() : events.OrderByDescending(e => e.Title).ToList(),
                "Category" => ascending ? events.OrderBy(e => e.Category).ToList() : events.OrderByDescending(e => e.Category).ToList(),
                "StartDate" => ascending ? events.OrderBy(e => e.StartDate).ToList() : events.OrderByDescending(e => e.StartDate).ToList(),
                "EndDate" => ascending ? events.OrderBy(e => e.EndDate).ToList() : events.OrderByDescending(e => e.EndDate).ToList(),
                "Venue" => ascending ? events.OrderBy(e => e.Venue?.Name).ToList() : events.OrderByDescending(e => e.Venue?.Name).ToList(),
                "Organizer" => ascending ? events.OrderBy(e => e.Organizer?.FirstName).ToList() : events.OrderByDescending(e => e.Organizer?.FirstName).ToList(),
                "ApprovalStatus" => ascending ? events.OrderBy(e => e.ApprovalStatus).ToList() : events.OrderByDescending(e => e.ApprovalStatus).ToList(),
                _ => events.ToList()
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetVenueAvailability(int venueId)
        {
            if (venueId <= 0)
                return BadRequest("Invalid venue ID");

            var bookedSlots = await _eventService.GetVenueAvailabilityAsync(venueId);

            var result = bookedSlots.Select(slot => new
            {
                start = slot.StartDate,
                end = slot.EndDate
            });

            return Json(result);
        }

        #endregion

        #region List & Filter

        // Replace your existing Index method in EventsController.cs with this:

        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, string? sortOption = null)
        {
            int pageSize = 5;

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
            int? userId = null;

            if (role == "Organizer" && User.Identity.IsAuthenticated)
                userId = GetCurrentUserId();

            string? sortColumn = null;
            string? sortDirection = "asc";

            // ✅ UPDATED: Parse sort option to include Governorate sorting
            if (!string.IsNullOrEmpty(sortOption))
            {
                if (sortOption.Contains("Asc"))
                    sortDirection = "asc";
                else if (sortOption.Contains("Desc"))
                    sortDirection = "desc";

                if (sortOption.StartsWith("Title"))
                    sortColumn = "Title";
                else if (sortOption.StartsWith("Price"))
                    sortColumn = "Price";
                else if (sortOption.StartsWith("StartDate"))
                    sortColumn = "StartDate";
                else if (sortOption.StartsWith("Governorate")) // ✅ NEW
                    sortColumn = "Governorate";
            }

            var (eventsList, totalEvents) = await _eventService.GetEventsForUserAsync(
                role,
                userId,
                searchTerm,
                sortColumn,
                sortDirection,
                page,
                pageSize
            );

            // Get photos AND videos for all events
            var eventPhotos = new Dictionary<int, List<string>>();
            var eventVideos = new Dictionary<int, List<string>>();

            foreach (var ev in eventsList)
            {
                var photos = _photoService.GetEventPhotos(ev.EventId);
                eventPhotos[ev.EventId] = photos.Select(p => p.FilePath).ToList();

                var videos = _photoService.GetEventVideos(ev.EventId);
                eventVideos[ev.EventId] = videos.Select(v => v.FilePath).ToList();
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalEvents / (double)pageSize);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortColumn = sortColumn;
            ViewBag.SortDirection = sortDirection;
            ViewBag.SortOption = sortOption;
            ViewBag.EventPhotos = eventPhotos;
            ViewBag.EventVideos = eventVideos;
            ViewBag.CurrentCategory = null;

            return View(eventsList);
        }

        [AllowAnonymous]
        public async Task<IActionResult> FilterByCategory(string category)
        {
            // 🧩 Determine current role
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
            int? userId = null;

            if (role == "Organizer" && User.Identity.IsAuthenticated)
                userId = GetCurrentUserId();

            List<EventBase> filtered;

            if (category != null && category.Equals("Trending", StringComparison.OrdinalIgnoreCase))
            {
                // 🏆 Get top 5 best-selling events
                filtered = await _eventService.GetTrendingEventsAsync(5);
            }
            else
            {
                // 🧠 Get events by category
                filtered = await _eventService.GetEventsByCategoryAsync(category);

                // ✅ Apply role-based filtering
                if (role == "Customer")
                {
                    var now = DateTime.Now;
                    filtered = filtered
                        .Where(e => e.StartDate > now) // 👈 Future events only
                        .ToList();
                }
            }

            // ✅ Get photos AND videos for filtered events
            var eventPhotos = new Dictionary<int, List<string>>();
            var eventVideos = new Dictionary<int, List<string>>();

            foreach (var ev in filtered)
            {
                var photos = _photoService.GetEventPhotos(ev.EventId);
                eventPhotos[ev.EventId] = photos.Select(p => p.FilePath).ToList();

                var videos = _photoService.GetEventVideos(ev.EventId);
                eventVideos[ev.EventId] = videos.Select(v => v.FilePath).ToList();
            }

            ViewBag.EventPhotos = eventPhotos;
            ViewBag.EventVideos = eventVideos;
            ViewBag.CurrentCategory = category;

            return View("Index", filtered);
        }

        #endregion

        #region Details

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var eventBase = await _eventService.GetEventByIdAsync(id);

            if (eventBase == null)
                return NotFound();

            // Authorization check for unpublished events
            if (!eventBase.IsPublished)
            {
                if (!User.IsInRole("Admin"))
                {
                    var currentUserId = GetCurrentUserId();
                    if (eventBase.OrganizerId != currentUserId)
                        return Forbid();
                }
            }

            // Load ticket tiers, discount codes, photos, and videos
            var ticketTiers = await _eventService.GetEventTicketTiersAsync(id);
            var discountCodes = await _eventService.GetEventDiscountCodesAsync(id);
            var photos = _photoService.GetEventPhotos(id);
            var videos = _photoService.GetEventVideos(id); // ✅ NEW

            // Load sub-event data
            var vm = new EventCreateVM
            {
                Event = eventBase
            };

            switch (eventBase.Category)
            {
                case "Concert":
                    vm.Concert = _unitOfWork.Concerts.GetById(id);
                    break;
                case "SportEvent":
                    vm.SportEvent = _unitOfWork.SportEvents.GetById(id);
                    break;
                case "TheaterPlay":
                    vm.TheaterPlay = _unitOfWork.TheaterPlays.GetById(id);
                    break;
                case "CulturalEvent":
                    vm.CulturalEvent = _unitOfWork.CulturalEvents.GetById(id);
                    break;
                case "TourismActivity":
                    vm.TourismActivity = _unitOfWork.TourismActivities.GetById(id);
                    break;
            }

            ViewBag.TicketTiers = ticketTiers;
            ViewBag.DiscountCodes = discountCodes;
            ViewBag.Photos = photos;
            ViewBag.Videos = videos; // ✅ NEW

            return View(vm);
        }

        #endregion

        #region Create

        [HttpGet]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public IActionResult Create()
        {
            var vm = new EventCreateVM
            {
                Organizers = _unitOfWork.Users.GetOrganizer(),
                Venues = _unitOfWork.Venues.GetApprovedVenues()
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> Create(EventCreateVM vm, List<IFormFile> photos, List<IFormFile> videos)
        {
            if (!ModelState.IsValid)
            {
                vm.Organizers = _unitOfWork.Users.GetOrganizer();
                vm.Venues = _unitOfWork.Venues.GetApprovedVenues();
                return View(vm);
            }

            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Create main event
            var result = await _eventService.CreateEventAsync(
                vm.Event,
                currentUserId,
                isAdmin,
                vm.AutoCreateTicketTiers);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                vm.Organizers = _unitOfWork.Users.GetOrganizer();
                vm.Venues = _unitOfWork.Venues.GetApprovedVenues();
                return View(vm);
            }

            var eventId = result.EventId!.Value;

            // Create sub-event
            await _eventService.CreateSubEventAsync(eventId, vm.Event.Category, GetSubEventData(vm));

            // ✅ Upload photos
            if (photos != null && photos.Any())
            {
                var photoUploadResult = await _photoService.UploadEventPhotosAsync(
                    eventId,
                    photos.Cast<object>(),
                    currentUserId);

                if (!photoUploadResult.Success)
                {
                    _logger.LogWarning($"Photo upload failed: {photoUploadResult.Message}");
                    TempData["Warning"] = $"Event created but photo upload failed: {photoUploadResult.Message}";
                }
            }

            // ✅ NEW: Upload videos
            if (videos != null && videos.Any())
            {
                var videoUploadResult = await _photoService.UploadEventVideosAsync(
                    eventId,
                    videos.Cast<object>(),
                    currentUserId);

                if (!videoUploadResult.Success)
                {
                    _logger.LogWarning($"Video upload failed: {videoUploadResult.Message}");
                    TempData["Warning"] = $"Event created but video upload failed: {videoUploadResult.Message}";
                }
            }

            // Create discount codes
            if (vm.DiscountCodes != null && vm.DiscountCodes.Any())
            {
                foreach (var discountVm in vm.DiscountCodes)
                {
                    if (string.IsNullOrWhiteSpace(discountVm.Code) || discountVm.DiscountValue <= 0)
                        continue;

                    var discount = new DiscountCode
                    {
                        EventId = eventId,
                        Code = discountVm.Code.Trim(),
                        DiscountType = discountVm.DiscountType,
                        DiscountValue = discountVm.DiscountValue,
                        ValidFrom = discountVm.ValidFrom,
                        ValidUntil = discountVm.ValidUntil,
                        MaxUsageCount = discountVm.MaxUsageCount,
                        IsActive = discountVm.IsActive
                    };

                    var createResult = await _eventService.CreateDiscountCodeAsync(discount, currentUserId);
                    if (!createResult.Success)
                    {
                        ModelState.AddModelError("", $"Failed to add code '{discountVm.Code}': {createResult.Message}");
                    }
                }

                await _unitOfWork.SaveAsync();
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _eventService.GetEventByIdNoTrackingAsync(id);
            if (ev == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = GetCurrentUserId();
                if (ev.OrganizerId != currentUserId)
                    return Forbid();
            }

            var vm = new EventCreateVM
            {
                Event = ev,
                Organizers = _unitOfWork.Users.GetOrganizer(),
                Venues = _unitOfWork.Venues.GetApprovedVenues()
            };

            // Load sub-event data
            switch (ev.Category)
            {
                case "Concert":
                    vm.Concert = _unitOfWork.Concerts.GetById(id);
                    break;
                case "SportEvent":
                    vm.SportEvent = _unitOfWork.SportEvents.GetById(id);
                    break;
                case "TheaterPlay":
                    vm.TheaterPlay = _unitOfWork.TheaterPlays.GetById(id);
                    break;
                case "CulturalEvent":
                    vm.CulturalEvent = _unitOfWork.CulturalEvents.GetById(id);
                    break;
                case "TourismActivity":
                    vm.TourismActivity = _unitOfWork.TourismActivities.GetById(id);
                    break;
            }

            // Load discount codes
            var discountCodes = await _eventService.GetEventDiscountCodesAsync(id);
            vm.DiscountCodes = discountCodes.Select(d => new DiscountCodeVM
            {
                DiscountCodeId = d.DiscountCodeId,
                Code = d.Code,
                DiscountType = d.DiscountType,
                DiscountValue = d.DiscountValue,
                ValidFrom = d.ValidFrom,
                ValidUntil = d.ValidUntil,
                MaxUsageCount = d.MaxUsageCount,
                IsActive = d.IsActive
            }).ToList();

            // Load photos and videos
            var photos = _photoService.GetEventPhotos(id);
            var videos = _photoService.GetEventVideos(id); // ✅ NEW

            ViewBag.Photos = photos;
            ViewBag.Videos = videos; // ✅ NEW

            return View(vm);
        }

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> Edit(int id, EventCreateVM vm, List<IFormFile> photos, List<IFormFile> videos)
        {
            if (id != vm.Event.EventId)
                return NotFound();

            if (!ModelState.IsValid)
            {
                vm.Organizers = _unitOfWork.Users.GetOrganizer();
                vm.Venues = _unitOfWork.Venues.GetApprovedVenues();
                return View(vm);
            }

            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Update main event
            var result = await _eventService.UpdateEventAsync(vm.Event, currentUserId, isAdmin);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                vm.Organizers = _unitOfWork.Users.GetOrganizer();
                vm.Venues = _unitOfWork.Venues.GetApprovedVenues();
                return View(vm);
            }

            // Update sub-event
            await _eventService.UpdateSubEventAsync(vm.Event.EventId, vm.Event.Category, GetSubEventData(vm));

            // ✅ Upload new photos
            if (photos != null && photos.Any())
            {
                var photoUploadResult = await _photoService.UploadEventPhotosAsync(
                    vm.Event.EventId,
                    photos.Cast<object>(),
                    currentUserId);

                if (!photoUploadResult.Success)
                {
                    _logger.LogWarning($"Photo upload failed: {photoUploadResult.Message}");
                }
            }

            // ✅ NEW: Upload new videos
            if (videos != null && videos.Any())
            {
                var videoUploadResult = await _photoService.UploadEventVideosAsync(
                    vm.Event.EventId,
                    videos.Cast<object>(),
                    currentUserId);

                if (!videoUploadResult.Success)
                {
                    _logger.LogWarning($"Video upload failed: {videoUploadResult.Message}");
                }
            }

            // Update discount codes
            if (vm.DiscountCodes != null)
            {
                var updateResult = await _eventService.UpdateDiscountCodesAsync(
                    vm.Event.EventId,
                    vm.DiscountCodes.Select(d => new DiscountCodeDTO
                    {
                        DiscountCodeId = d.DiscountCodeId,
                        Code = d.Code,
                        DiscountType = d.DiscountType,
                        DiscountValue = d.DiscountValue,
                        ValidFrom = d.ValidFrom,
                        ValidUntil = d.ValidUntil,
                        MaxUsageCount = d.MaxUsageCount,
                        IsActive = d.IsActive
                    }).ToList(),
                    currentUserId
                );

                if (!updateResult.Success)
                {
                    _logger.LogWarning(updateResult.Message);
                }
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Delete

        [HttpGet]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _eventService.GetEventByIdAsync(id);
            if (ev == null) return NotFound();

            if (!User.IsInRole("Admin"))
            {
                var currentUserId = GetCurrentUserId();
                if (ev.OrganizerId != currentUserId)
                    return Forbid();
            }

            return View(ev);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _eventService.DeleteEventAsync(id, currentUserId, isAdmin);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Photo Management API

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.DeleteEventPhotoAsync(photoId, currentUserId, isAdmin);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> SetPrimaryPhoto(int eventId, int photoId)
        {
            var result = await _photoService.SetEventPrimaryPhotoAsync(eventId, photoId);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Video Management API

        /// <summary>
        /// Upload videos for an event (called from Create/Edit forms)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> UploadEventVideos(int eventId, List<IFormFile> videos)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.UploadEventVideosAsync(
                eventId,
                videos.Cast<object>(),
                currentUserId);

            return Json(new { success = result.Success, message = result.Message, videoIds = result.VideoIds });
        }

        /// <summary>
        /// Delete an event video
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> DeleteEventVideo(int videoId)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.DeleteEventVideoAsync(videoId, currentUserId, isAdmin);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Partial Views for Sub-Events

        [HttpGet]
        public IActionResult LoadCategoryForm(string category)
        {
            if (string.IsNullOrEmpty(category))
                return Content("");

            string viewName = category switch
            {
                "Concert" => "_ConcertForm",
                "SportEvent" => "_SportEventForm",
                "TheaterPlay" => "_TheaterPlayForm",
                "CulturalEvent" => "_CulturalEventForm",
                "TourismActivity" => "_TourismActivityForm",
                _ => "_DefaultForm"
            };

            return PartialView($"Partials/{viewName}");
        }

        public IActionResult LoadCategoryFormForEdit(string category, int eventId)
        {
            switch (category)
            {
                case "Concert":
                    var concert = _unitOfWork.Concerts.GetById(eventId);
                    return PartialView("Partials/_ConcertForm", concert ?? new Concert());

                case "SportEvent":
                    var sportEvent = _unitOfWork.SportEvents.GetById(eventId);
                    return PartialView("Partials/_SportEventForm", sportEvent ?? new SportEvent());

                case "CulturalEvent":
                    var culturalEvent = _unitOfWork.CulturalEvents.GetById(eventId);
                    return PartialView("Partials/_CulturalEventForm", culturalEvent ?? new CulturalEvent());

                case "TheaterPlay":
                    var theaterPlay = _unitOfWork.TheaterPlays.GetById(eventId);
                    return PartialView("Partials/_TheaterPlayForm", theaterPlay ?? new TheaterPlay());

                case "TourismActivity":
                    var tourismActivity = _unitOfWork.TourismActivities.GetById(eventId);
                    return PartialView("Partials/_TourismActivityForm", tourismActivity ?? new TourismActivity());

                default:
                    return Content("");
            }
        }

        public IActionResult LoadCategoryDetails(string category, int id)
        {
            switch (category)
            {
                case "Concert":
                    var concert = _unitOfWork.Concerts.GetById(id);
                    if (concert == null)
                        return Content("<p class='text-danger'>Concert not found.</p>");
                    return PartialView("Partials/_ConcertDetails", concert);

                case "SportEvent":
                    var sportEvent = _unitOfWork.SportEvents.GetById(id);
                    if (sportEvent == null)
                        return Content("<p class='text-danger'>Sport event not found.</p>");
                    return PartialView("Partials/_SportEventDetails", sportEvent);

                case "CulturalEvent":
                    var culturalEvent = _unitOfWork.CulturalEvents.GetById(id);
                    if (culturalEvent == null)
                        return Content("<p class='text-danger'>Cultural event not found.</p>");
                    return PartialView("Partials/_CulturalEventDetails", culturalEvent);

                case "TheaterPlay":
                    var theaterPlay = _unitOfWork.TheaterPlays.GetById(id);
                    if (theaterPlay == null)
                        return Content("<p class='text-danger'>Theater play not found.</p>");
                    return PartialView("Partials/_TheaterPlayDetails", theaterPlay);

                case "TourismActivity":
                    var tourismActivity = _unitOfWork.TourismActivities.GetById(id);
                    if (tourismActivity == null)
                        return Content("<p class='text-danger'>Tourism activity not found.</p>");
                    return PartialView("Partials/_TourismActivityDetails", tourismActivity);

                default:
                    return Content("");
            }
        }

        #endregion

        #region Private Helper

        private object? GetSubEventData(EventCreateVM vm)
        {
            return vm.Event.Category switch
            {
                "Concert" => vm.Concert,
                "SportEvent" => vm.SportEvent,
                "TheaterPlay" => vm.TheaterPlay,
                "CulturalEvent" => vm.CulturalEvent,
                "TourismActivity" => vm.TourismActivity,
                _ => null
            };
        }

        #endregion
    }
}