using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Entertania1.ViewModels.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    [Authorize(Policy = "ActiveOrganizerOrAdmin")]
    public class VenueController : Controller
    {
        private readonly IVenueService _venueService;
        private readonly IPhotoGalleryService _photoService;
        private readonly ILogger<VenueController> _logger;

        public VenueController(
            IVenueService venueService,
            IPhotoGalleryService photoService,
            ILogger<VenueController> logger)
        {
            _venueService = venueService;
            _photoService = photoService;
            _logger = logger;
        }

        #region Index
        // ✅ UPDATED: Added governorate filter parameter
        public async Task<IActionResult> Index(
            string searchTerm,
            string sortOrder,
            int? governorate,  // ✅ NEW: Governorate filter
            int page = 1)
        {
            try
            {
                int pageSize = 5;
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                // Get domain data from service
                var domainData = await _venueService.GetVenuesAsync(
                    searchTerm,
                    sortOrder,
                    governorate,  // ✅ NEW: Pass governorate filter
                    page,
                    pageSize,
                    currentUserId,
                    isAdmin);

                // Filter out deleted venues for non-admins
                if (!isAdmin)
                {
                    domainData.Venues = domainData.Venues
                        .Where(v => !v.IsDeleted)
                        .ToList();
                }

                // Get photos AND videos for all venues
                var venuePhotos = new Dictionary<int, List<string>>();
                var venueVideos = new Dictionary<int, List<string>>();

                foreach (var venue in domainData.Venues)
                {
                    var photos = _photoService.GetVenuePhotos(venue.VenueId);
                    venuePhotos[venue.VenueId] = photos.Select(p => p.FilePath).ToList();

                    var videos = _photoService.GetVenueVideos(venue.VenueId);
                    venueVideos[venue.VenueId] = videos.Select(v => v.FilePath).ToList();
                }

                // Map to ViewModel
                var viewModel = new VenueListVM
                {
                    Venues = domainData.Venues,
                    SearchTerm = domainData.SearchTerm,
                    SortOrder = domainData.SortOrder,
                    CurrentPage = domainData.CurrentPage,
                    TotalPages = domainData.TotalPages
                };

                // Set sort parameters for view
                ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                ViewData["LocationSortParm"] = sortOrder == "location" ? "location_desc" : "location";
                ViewData["CapacitySortParm"] = sortOrder == "capacity" ? "capacity_desc" : "capacity";

                // ✅ NEW: Pass governorate filter to view
                ViewBag.SelectedGovernorate = governorate;

                ViewBag.VenuePhotos = venuePhotos;
                ViewBag.VenueVideos = venueVideos;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venues list");
                TempData["Error"] = "An error occurred while loading venues";
                return View(new VenueListVM());
            }
        }
        #endregion

        #region Details
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var (success, venue, events, errorMessage) = await _venueService.GetVenueDetailsAsync(
                    id,
                    currentUserId,
                    isAdmin);

                if (!success)
                {
                    if (errorMessage.Contains("permission"))
                        return Forbid();

                    return NotFound(errorMessage);
                }

                var photos = _photoService.GetVenuePhotos(id);
                var videos = _photoService.GetVenueVideos(id);

                ViewBag.VenueEvents = events;
                ViewBag.Photos = photos;
                ViewBag.Videos = videos;

                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading venue details for venue {id}");
                TempData["Error"] = "An error occurred while loading venue details";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        #region Create

        public IActionResult Create()
        {
            var vm = new VenueCreateViewModel();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VenueCreateViewModel vm, List<IFormFile> photos, List<IFormFile> videos)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                int? organizerId = null;
                bool isAdmin = User.IsInRole("Admin");

                if (User.IsInRole("Organizer"))
                {
                    organizerId = GetCurrentUserId();
                }
                else if (isAdmin)
                {
                    organizerId = null;
                }

                // Map ViewModel to domain data
                var domainData = new VenueCreationData
                {
                    Name = vm.Name,
                    Location = vm.Location,
                    Capacity = vm.Capacity,
                    Governorate = vm.Governorate,  // ✅ NEW: Include governorate
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,
                    GoogleMapsUrl = vm.GoogleMapsUrl
                };

                var (success, venueId, message) = await _venueService.CreateVenueAsync(domainData, organizerId, isAdmin);

                if (success)
                {
                    // Upload photos if provided
                    if (photos != null && photos.Any())
                    {
                        var photoUploadResult = await _photoService.UploadVenuePhotosAsync(
                            venueId,
                            photos.Cast<object>(),
                            organizerId ?? GetCurrentUserId() ?? 0);

                        if (!photoUploadResult.Success)
                        {
                            _logger.LogWarning($"Photo upload failed: {photoUploadResult.Message}");
                            TempData["Warning"] = $"Venue created but photo upload failed: {photoUploadResult.Message}";
                        }
                    }

                    // Upload videos if provided
                    if (videos != null && videos.Any())
                    {
                        var videoUploadResult = await _photoService.UploadVenueVideosAsync(
                            venueId,
                            videos.Cast<object>(),
                            organizerId ?? GetCurrentUserId() ?? 0);

                        if (!videoUploadResult.Success)
                        {
                            _logger.LogWarning($"Video upload failed: {videoUploadResult.Message}");
                            TempData["Warning"] = $"Venue created but video upload failed: {videoUploadResult.Message}";
                        }
                    }

                    TempData["Success"] = message;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", message);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue");
                ModelState.AddModelError("", "An error occurred while creating the venue");
                return View(vm);
            }
        }
        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var (success, venue, errorMessage) = await _venueService.GetVenueForEditAsync(
                    id,
                    currentUserId,
                    isAdmin);

                if (!success)
                {
                    if (errorMessage.Contains("permission"))
                        return Forbid();

                    return NotFound(errorMessage);
                }

                // Map domain model to ViewModel
                var vm = new VenueCreateViewModel
                {
                    VenueId = venue!.VenueId,
                    Name = venue.Name ?? string.Empty,
                    Location = venue.Location ?? string.Empty,
                    Capacity = venue.Capacity,
                    Governorate = venue.Governorate,  // ✅ NEW: Include governorate
                    Latitude = venue.Latitude,
                    Longitude = venue.Longitude,
                    GoogleMapsUrl = venue.GoogleMapsUrl
                };

                var photos = _photoService.GetVenuePhotos(id);
                var videos = _photoService.GetVenueVideos(id);

                ViewBag.Photos = photos;
                ViewBag.Videos = videos;

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading venue for edit: {id}");
                TempData["Error"] = "An error occurred while loading the venue";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VenueCreateViewModel vm, List<IFormFile> photos, List<IFormFile> videos)
        {
            if (id != vm.VenueId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var existingPhotos = _photoService.GetVenuePhotos(id);
                var existingVideos = _photoService.GetVenueVideos(id);
                ViewBag.Photos = existingPhotos;
                ViewBag.Videos = existingVideos;
                return View(vm);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                // Map ViewModel to domain data
                var domainData = new VenueCreationData
                {
                    Name = vm.Name,
                    Location = vm.Location,
                    Capacity = vm.Capacity,
                    Governorate = vm.Governorate,  // ✅ NEW: Include governorate
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,
                    GoogleMapsUrl = vm.GoogleMapsUrl
                };

                var (success, message) = await _venueService.UpdateVenueAsync(
                    id,
                    domainData,
                    currentUserId,
                    isAdmin);

                if (success)
                {
                    // Upload new photos if provided
                    if (photos != null && photos.Any())
                    {
                        var photoUploadResult = await _photoService.UploadVenuePhotosAsync(
                            id,
                            photos.Cast<object>(),
                            currentUserId ?? 0);

                        if (!photoUploadResult.Success)
                        {
                            _logger.LogWarning($"Photo upload failed: {photoUploadResult.Message}");
                            TempData["Warning"] = $"Venue updated but photo upload failed: {photoUploadResult.Message}";
                        }
                    }

                    // Upload new videos if provided
                    if (videos != null && videos.Any())
                    {
                        var videoUploadResult = await _photoService.UploadVenueVideosAsync(
                            id,
                            videos.Cast<object>(),
                            currentUserId ?? 0);

                        if (!videoUploadResult.Success)
                        {
                            _logger.LogWarning($"Video upload failed: {videoUploadResult.Message}");
                            TempData["Warning"] = $"Venue updated but video upload failed: {videoUploadResult.Message}";
                        }
                    }

                    TempData["Success"] = message;
                    return RedirectToAction(nameof(Index));
                }

                if (message.Contains("permission"))
                    return Forbid();

                ModelState.AddModelError("", message);

                var existingPhotos = _photoService.GetVenuePhotos(id);
                var existingVideos = _photoService.GetVenueVideos(id);
                ViewBag.Photos = existingPhotos;
                ViewBag.Videos = existingVideos;

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating venue {id}");
                ModelState.AddModelError("", "An error occurred while updating the venue");
                return View(vm);
            }
        }
        #endregion

        #region Delete

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var (success, venue, errorMessage) = await _venueService.GetVenueForEditAsync(
                    id,
                    currentUserId,
                    isAdmin);

                if (!success)
                {
                    if (errorMessage.Contains("permission"))
                        return Forbid();

                    return NotFound(errorMessage);
                }

                return View(venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading venue for delete: {id}");
                TempData["Error"] = "An error occurred while loading the venue";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var isAdmin = User.IsInRole("Admin");

                var (success, message) = await _venueService.DeleteVenueAsync(
                    id,
                    currentUserId,
                    isAdmin);

                if (success)
                {
                    TempData["Success"] = message;
                }
                else
                {
                    if (message.Contains("permission"))
                        return Forbid();

                    TempData["Error"] = message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting venue {id}");
                TempData["Error"] = "An error occurred while deleting the venue";
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Photo Management API
        [HttpPost]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.DeleteVenuePhotoAsync(photoId, currentUserId ?? 0, isAdmin);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> SetPrimaryPhoto(int venueId, int photoId)
        {
            var result = await _photoService.SetVenuePrimaryPhotoAsync(venueId, photoId);

            return Json(new { success = result.Success, message = result.Message });
        }
        #endregion

        #region Video Management API

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> UploadVenueVideos(int venueId, List<IFormFile> videos)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.UploadVenueVideosAsync(
                venueId,
                videos.Cast<object>(),
                currentUserId ?? 0);

            return Json(new { success = result.Success, message = result.Message, videoIds = result.VideoIds });
        }

        [HttpPost]
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var result = await _photoService.DeleteVenueVideoAsync(videoId, currentUserId ?? 0, isAdmin);

            return Json(new { success = result.Success, message = result.Message });
        }

        #endregion

        #region Helpers
        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
                return userId;
            return null;
        }
        #endregion
    }
}