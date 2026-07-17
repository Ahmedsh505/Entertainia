using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Entertania1.Models;
using Entertania1.ViewModels.Event;
using Infrastructure.Data.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEventService _eventService;
        private readonly IPhotoGalleryService _photoService;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IEventService eventService, IPhotoGalleryService photoService, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _eventService = eventService;
            _photoService = photoService;
            _unitOfWork = unitOfWork;

        }

        # region Helper Methods
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out var currentUserId);
            return currentUserId;
        }
        #endregion

        public async Task<IActionResult> Index()
        {
            var trendingEvents = await _eventService.GetTrendingEventsAsync(5);
            var eventPhotos = new Dictionary<int, List<string>>();
            var eventVideos = new Dictionary<int, List<string>>();

            foreach (var ev in trendingEvents)
            {
                var photos = _photoService.GetEventPhotos(ev.EventId);
                var videos = _photoService.GetEventVideos(ev.EventId);
                eventPhotos[ev.EventId] = photos.Select(p => p.FilePath).ToList();
                eventVideos[ev.EventId] = videos.Select(v => v.FilePath).ToList();
            }

            ViewBag.EventPhotos = eventPhotos;
            ViewBag.EventVideos = eventVideos;

            // Load top reviews
            var topReviews = await _unitOfWork.Reviews.GetTopReviewsAsync(10);
            ViewBag.Reviews = topReviews;

            return View(trendingEvents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(string name, string email, string phoneNumber, int rating, string comment)
        {
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Please provide a valid rating and comment.";
                return RedirectToAction("Index");
            }

            var review = new Core.Models.Review
            {
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            if (User.Identity.IsAuthenticated)
            {
                var userId = GetCurrentUserId();
                var user = _unitOfWork.Users.GetById(userId);
                review.UserId = userId;
                review.Name = $"{user.FirstName} {user.LastName}";
                review.Email = user.Email;
                review.PhoneNumber = user.PhoneNumber;
                review.ProfilePicturePath = user.ProfilePicturePath;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Name and email are required.";
                    return RedirectToAction("Index");
                }

                review.Name = name.Trim();
                review.Email = email.Trim();
                review.PhoneNumber = phoneNumber?.Trim();
            }

            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveAsync();

            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            await _unitOfWork.Reviews.DeleteAsync(id);
            await _unitOfWork.SaveAsync();
            return Json(new { success = true, message = "Review deleted successfully" });
        }

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
            var videos = _photoService.GetEventVideos(id); // ? NEW

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
            ViewBag.Videos = videos; // ? NEW

            return View("Details", vm);
        }

        public IActionResult Abous_Us()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
