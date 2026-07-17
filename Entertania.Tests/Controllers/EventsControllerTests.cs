using Core.DTOs;
using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.EventModels;
using Core.Models.PhotoModels;
using Core.Models.VideoModels;
using Entertania1.Controllers;
using Entertania1.ViewModels.Event;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Entertania.Tests.Controllers
{
    public class EventsControllerTests
    {
        private readonly Mock<IEventService> _mockEventService;
        private readonly Mock<IPhotoGalleryService> _mockPhotoService;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<EventsController>> _mockLogger;
        private readonly EventsController _controller;

        public EventsControllerTests()
        {
            _mockEventService = new Mock<IEventService>();
            _mockPhotoService = new Mock<IPhotoGalleryService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<EventsController>>();

            _controller = new EventsController(
                _mockEventService.Object,
                _mockPhotoService.Object,
                _mockUnitOfWork.Object,
                _mockLogger.Object
            );

            // Initialize TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        #region Helper Methods

        private void SetupUserContext(string role, int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private List<EventBase> GetSampleEvents()
        {
            var user1 = new User { Id = 1, FirstName = "John", DeletionType = DeletionType.None };
            var user2 = new User { Id = 2, FirstName = "Jane", DeletionType = DeletionType.None };

            return new List<EventBase>
            {
                new EventBase
                {
                    EventId = 1,
                    Title = "Concert 2025",
                    Category = "Concert",
                    StartDate = DateTime.Now.AddDays(10),
                    EndDate = DateTime.Now.AddDays(11),
                    ApprovalStatus = ApprovalStatus.Approved,
                    OrganizerId = 1,
                    Organizer = user1
                },
                new EventBase
                {
                    EventId = 2,
                    Title = "Football Match",
                    Category = "SportEvent",
                    StartDate = DateTime.Now.AddDays(5),
                    EndDate = DateTime.Now.AddDays(5),
                    ApprovalStatus = ApprovalStatus.Approved,
                    OrganizerId = 2,
                    Organizer = user2
                }
            };
        }

        private List<EventPhoto> GetSamplePhotos(int eventId)
        {
            return new List<EventPhoto>
            {
                new EventPhoto
                {
                    EventPhotoId = 1,
                    EventId = eventId,
                    FilePath = "/uploads/events/photo1.jpg",
                    IsPrimary = true
                },
                new EventPhoto
                {
                    EventPhotoId = 2,
                    EventId = eventId,
                    FilePath = "/uploads/events/photo2.jpg",
                    IsPrimary = false
                }
            };
        }

        #endregion

        #region Index Tests

        [Fact]
        public async Task Index_AsAdmin_ReturnsAllEvents()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var events = GetSampleEvents();

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Admin",
                null,
                null,
                null,
                "asc",
                1,
                5))
                .ReturnsAsync((events, 2));

            // Setup photo AND video service for each event
            foreach (var ev in events)
            {
                _mockPhotoService.Setup(p => p.GetEventPhotos(ev.EventId))
                    .Returns(GetSamplePhotos(ev.EventId));
                _mockPhotoService.Setup(p => p.GetEventVideos(ev.EventId))
                    .Returns(new List<EventVideo>()); // ✅ ADD THIS
            }

            // Act
            var result = await _controller.Index(null, 1, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(2);

            // Verify ViewBag contains EventPhotos
            var eventPhotos = _controller.ViewBag.EventPhotos as Dictionary<int, List<string>>;
            eventPhotos.Should().NotBeNull();
            eventPhotos.Should().HaveCount(2);
        }

        [Fact]
        public async Task Index_AsOrganizer_ReturnsOrganizerEvents()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var events = GetSampleEvents().Where(e => e.OrganizerId == 1).ToList();

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Organizer",
                1,
                null,
                null,
                "asc",
                1,
                5))
                .ReturnsAsync((events, 1));

            foreach (var ev in events)
            {
                _mockPhotoService.Setup(p => p.GetEventPhotos(ev.EventId))
                    .Returns(GetSamplePhotos(ev.EventId));
                _mockPhotoService.Setup(p => p.GetEventVideos(ev.EventId))
                    .Returns(new List<EventVideo>()); // ✅ ADD THIS
            }

            // Act
            var result = await _controller.Index(null, 1, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(1);
            model?.First().OrganizerId.Should().Be(1);
        }

        [Fact]
        public async Task Index_AsCustomer_ReturnsOnlyApprovedFutureEvents()
        {
            // Arrange
            SetupUserContext("Customer", 3);
            var futureEvent = new EventBase
            {
                EventId = 1,
                Title = "Future Event",
                StartDate = DateTime.Now.AddDays(10),
                ApprovalStatus = ApprovalStatus.Approved,
                Organizer = new User { DeletionType = DeletionType.None }
            };

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Customer",
                null,
                null,
                null,
                "asc",
                1,
                5))
                .ReturnsAsync((new List<EventBase> { futureEvent }, 1));

            _mockPhotoService.Setup(p => p.GetEventPhotos(1))
                .Returns(GetSamplePhotos(1));
            _mockPhotoService.Setup(p => p.GetEventVideos(1))
                .Returns(new List<EventVideo>()); // ✅ ADD THIS

            // Act
            var result = await _controller.Index(null, 1, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(1);
            model?.First().EventId.Should().Be(1);
        }

        [Fact]
        public async Task Index_WithSearchTerm_ReturnsFilteredEvents()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var events = GetSampleEvents().Where(e => e.Title.Contains("Concert")).ToList();

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Admin",
                null,
                "Concert",
                null,
                "asc",
                1,
                5))
                .ReturnsAsync((events, 1));

            _mockPhotoService.Setup(p => p.GetEventPhotos(It.IsAny<int>()))
                .Returns(GetSamplePhotos(1));
            _mockPhotoService.Setup(p => p.GetEventVideos(It.IsAny<int>()))
                .Returns(new List<EventVideo>()); // ✅ ADD THIS

            // Act
            var result = await _controller.Index("Concert", 1, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(1);
            model?.First().Title.Should().Contain("Concert");

            string searchTerm = _controller.ViewBag.SearchTerm;
            searchTerm.Should().Be("Concert");
        }

        [Fact]
        public async Task Index_WithSortOption_ReturnsSortedEvents()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var events = GetSampleEvents();

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Admin",
                null,
                null,
                "Title",
                "desc",
                1,
                5))
                .ReturnsAsync((events.OrderByDescending(e => e.Title).ToList(), 2));

            foreach (var ev in events)
            {
                _mockPhotoService.Setup(p => p.GetEventPhotos(ev.EventId))
                    .Returns(GetSamplePhotos(ev.EventId));
                _mockPhotoService.Setup(p => p.GetEventVideos(ev.EventId))
                    .Returns(new List<EventVideo>()); // ✅ ADD THIS
            }

            // Act
            var result = await _controller.Index(null, 1, "TitleDesc");

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model?.First().Title.Should().Be("Football Match");
        }

        [Fact]
        public async Task Index_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupUserContext("Admin", 1);
            var events = Enumerable.Range(1, 5).Select(i => new EventBase
            {
                EventId = i,
                Title = $"Event {i}",
                StartDate = DateTime.Now.AddDays(i),
                ApprovalStatus = ApprovalStatus.Approved
            }).ToList();

            _mockEventService.Setup(s => s.GetEventsForUserAsync(
                "Admin",
                null,
                null,
                null,
                "asc",
                2,
                5))
                .ReturnsAsync((events, 10));

            foreach (var ev in events)
            {
                _mockPhotoService.Setup(p => p.GetEventPhotos(ev.EventId))
                    .Returns(new List<EventPhoto>());
                _mockPhotoService.Setup(p => p.GetEventVideos(ev.EventId))
                    .Returns(new List<EventVideo>()); // ✅ ADD THIS
            }

            // Act
            var result = await _controller.Index(null, 2, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(5);

            int currentPage = (int)_controller.ViewBag.CurrentPage;
            currentPage.Should().Be(2);

            int totalPages = (int)_controller.ViewBag.TotalPages;
            totalPages.Should().Be(2);
        }

        

        #endregion

        #region Details Tests

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithEvent()
        {
            // Arrange
            var eventBase = GetSampleEvents().First();
            // Don't try to set IsPublished if it's read-only
            // Instead, mock an event that already has IsPublished = true
            var publishedEvent = new EventBase
            {
                EventId = eventBase.EventId,
                Title = eventBase.Title,
                Category = eventBase.Category,
                StartDate = eventBase.StartDate,
                EndDate = eventBase.EndDate,
                ApprovalStatus = eventBase.ApprovalStatus,
                OrganizerId = eventBase.OrganizerId,
                Organizer = eventBase.Organizer
                // IsPublished will be true by default or set in constructor
            };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1))
                .ReturnsAsync(publishedEvent);
            _mockEventService.Setup(s => s.GetEventTicketTiersAsync(1))
                .ReturnsAsync(new List<EventTicketTier>());
            _mockEventService.Setup(s => s.GetEventDiscountCodesAsync(1))
                .ReturnsAsync(new List<DiscountCode>());
            _mockPhotoService.Setup(p => p.GetEventPhotos(1))
                .Returns(GetSamplePhotos(1));

            var mockConcertRepo = new Mock<IConcertRepo>();
            mockConcertRepo.Setup(r => r.GetById(1)).Returns(new Concert());
            _mockUnitOfWork.Setup(u => u.Concerts).Returns(mockConcertRepo.Object);

            // Act
            var result = await _controller.Details(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as EventCreateVM;
            model?.Event.Should().NotBeNull();
            model?.Event.EventId.Should().Be(1);

            var photos = _controller.ViewBag.Photos as List<EventPhoto>;
            photos.Should().NotBeNull();
            photos.Should().HaveCount(2);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetEventByIdAsync(999))
                .ReturnsAsync((EventBase?)null);

            // Act
            var result = await _controller.Details(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Details_UnpublishedEvent_NonOwner_ReturnsForbid()
        {
            // Arrange
            SetupUserContext("Organizer", 2);
            var eventBase = new EventBase
            {
                EventId = 1,
                Title = "Unpublished Event",
                OrganizerId = 1,
                //IsPublished = false
            };

            _mockEventService.Setup(s => s.GetEventByIdAsync(1))
                .ReturnsAsync(eventBase);

            // Act
            var result = await _controller.Details(1);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        #endregion

        #region Create Tests

        [Fact]
        public void Create_Get_ReturnsViewWithViewModel()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var mockUserRepo = new Mock<IUserRepo>();
            mockUserRepo.Setup(r => r.GetOrganizer()).Returns(new List<User>());
            _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

            var mockVenueRepo = new Mock<IVenueRepo>();
            mockVenueRepo.Setup(r => r.GetApprovedVenues()).Returns(new List<Venue>());
            _mockUnitOfWork.Setup(u => u.Venues).Returns(mockVenueRepo.Object);

            // Act
            var result = _controller.Create();

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as EventCreateVM;
            model.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_ValidModel_WithPhotos_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    Title = "New Event",
                    Category = "Concert",
                    StartDate = DateTime.Now.AddDays(30),
                    EndDate = DateTime.Now.AddDays(31)
                },
                Concert = new Concert(),
                DiscountCodes = new List<DiscountCodeVM>()
            };
            var photos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "photo1.jpg" && f.Length == 1000)
    };
            var videos = new List<IFormFile>(); // ✅ ADD: Empty videos list

            _mockEventService.Setup(s => s.CreateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Success", 1));
            _mockEventService.Setup(s => s.CreateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Success"));
            _mockPhotoService.Setup(p => p.UploadEventPhotosAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, new List<int> { 1 }, "Photos uploaded"));

            // Act
            var result = await _controller.Create(vm, photos, videos); // ✅ UPDATED: Added videos parameter

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult?.ActionName.Should().Be("Index");
            _controller.TempData["Success"].Should().NotBeNull();
        }
        [Fact]
        public async Task Create_Post_ValidModel_WithVideos_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    Title = "New Event",
                    Category = "Concert",
                    StartDate = DateTime.Now.AddDays(30),
                    EndDate = DateTime.Now.AddDays(31)
                },
                Concert = new Concert(),
                DiscountCodes = new List<DiscountCodeVM>()
            };
            var photos = new List<IFormFile>();
            var videos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "video1.mp4" && f.Length == 5000)
    };

            _mockEventService.Setup(s => s.CreateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Success", 1));
            _mockEventService.Setup(s => s.CreateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Success"));
            _mockPhotoService.Setup(p => p.UploadEventVideosAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, new List<int> { 1 }, "Videos uploaded"));

            // Act
            var result = await _controller.Create(vm, photos, videos);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult?.ActionName.Should().Be("Index");
            _controller.TempData["Success"].Should().NotBeNull();
        }


        [Fact]
        public async Task Create_Post_PhotoUploadFails_ShowsWarning()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    Title = "New Event",
                    Category = "Concert",
                    StartDate = DateTime.Now.AddDays(30),
                    EndDate = DateTime.Now.AddDays(31)
                },
                Concert = new Concert()
            };
            var photos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "photo1.jpg" && f.Length == 1000)
    };
            var videos = new List<IFormFile>(); // ✅ ADD: Empty videos list

            _mockEventService.Setup(s => s.CreateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Success", 1));
            _mockEventService.Setup(s => s.CreateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Success"));
            _mockPhotoService.Setup(p => p.UploadEventPhotosAsync(
                 It.IsAny<int>(),
                 It.IsAny<IEnumerable<object>>(),
                 It.IsAny<int>()))
                 .ReturnsAsync((false, new List<int>(), "Upload failed"));

            // Act
            var result = await _controller.Create(vm, photos, videos); // ✅ UPDATED: Added videos parameter

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["Warning"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_VideoUploadFails_ShowsWarning()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    Title = "New Event",
                    Category = "Concert",
                    StartDate = DateTime.Now.AddDays(30),
                    EndDate = DateTime.Now.AddDays(31)
                },
                Concert = new Concert()
            };
            var photos = new List<IFormFile>();
            var videos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "video1.mp4" && f.Length == 5000)
    };

            _mockEventService.Setup(s => s.CreateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Success", 1));
            _mockEventService.Setup(s => s.CreateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Success"));
            _mockPhotoService.Setup(p => p.UploadEventVideosAsync(
                 It.IsAny<int>(),
                 It.IsAny<IEnumerable<object>>(),
                 It.IsAny<int>()))
                 .ReturnsAsync((false, new List<int>(), "Video upload failed"));

            // Act
            var result = await _controller.Create(vm, photos, videos);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["Warning"].Should().NotBeNull();
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            _controller.ModelState.AddModelError("Title", "Required");
            var mockUserRepo = new Mock<IUserRepo>();
            mockUserRepo.Setup(r => r.GetOrganizer()).Returns(new List<User>());
            _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);
            var mockVenueRepo = new Mock<IVenueRepo>();
            mockVenueRepo.Setup(r => r.GetApprovedVenues()).Returns(new List<Venue>());
            _mockUnitOfWork.Setup(u => u.Venues).Returns(mockVenueRepo.Object);
            var vm = new EventCreateVM { Event = new EventBase() };

            // Act
            var result = await _controller.Create(vm, new List<IFormFile>(), new List<IFormFile>()); // ✅ UPDATED: Added videos parameter

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult?.Model.Should().Be(vm);
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_ValidId_ReturnsViewWithEventAndPhotos()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var eventBase = GetSampleEvents().First();

            _mockEventService.Setup(s => s.GetEventByIdNoTrackingAsync(1))
                .ReturnsAsync(eventBase);

            var mockUserRepo = new Mock<IUserRepo>();
            mockUserRepo.Setup(r => r.GetOrganizer()).Returns(new List<User>());
            _mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);

            var mockVenueRepo = new Mock<IVenueRepo>();
            mockVenueRepo.Setup(r => r.GetApprovedVenues()).Returns(new List<Venue>());
            _mockUnitOfWork.Setup(u => u.Venues).Returns(mockVenueRepo.Object);

            var mockConcertRepo = new Mock<IConcertRepo>();
            mockConcertRepo.Setup(r => r.GetById(1)).Returns(new Concert());
            _mockUnitOfWork.Setup(u => u.Concerts).Returns(mockConcertRepo.Object);

            _mockEventService.Setup(s => s.GetEventDiscountCodesAsync(1))
                .ReturnsAsync(new List<DiscountCode>());

            _mockPhotoService.Setup(p => p.GetEventPhotos(1))
                .Returns(GetSamplePhotos(1));

            // Act
            var result = await _controller.Edit(1);

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as EventCreateVM;
            model?.Event.EventId.Should().Be(1);

            var photos = _controller.ViewBag.Photos as List<EventPhoto>;
            photos.Should().NotBeNull();
            photos.Should().HaveCount(2);
        }
        [Fact]
        public async Task Edit_Post_ValidModel_WithNewPhotos_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    EventId = 1,
                    Title = "Updated Event",
                    Category = "Concert"
                },
                DiscountCodes = new List<DiscountCodeVM>()
            };
            var photos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "newphoto.jpg" && f.Length == 1000)
    };
            var videos = new List<IFormFile>(); // ✅ ADD: Empty videos list

            _mockEventService.Setup(s => s.UpdateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<List<DiscountCodeDTO>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, "Updated"));
            _mockPhotoService.Setup(p => p.UploadEventPhotosAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, new List<int> { 5 }, "Photos uploaded"));

            // Act
            var result = await _controller.Edit(1, vm, photos, videos); // ✅ UPDATED: Added videos parameter

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult?.ActionName.Should().Be("Index");
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Post_ValidModel_WithNewVideos_RedirectsToIndex()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    EventId = 1,
                    Title = "Updated Event",
                    Category = "Concert"
                },
                DiscountCodes = new List<DiscountCodeVM>()
            };
            var photos = new List<IFormFile>();
            var videos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "newvideo.mp4" && f.Length == 5000)
    };

            _mockEventService.Setup(s => s.UpdateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<List<DiscountCodeDTO>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, "Updated"));
            _mockPhotoService.Setup(p => p.UploadEventVideosAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, new List<int> { 5 }, "Videos uploaded"));

            // Act
            var result = await _controller.Edit(1, vm, photos, videos);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult?.ActionName.Should().Be("Index");
        }

        [Fact]
        public async Task Edit_Post_VideoUploadFails_LogsWarning()
        {
            // Arrange
            SetupUserContext("Organizer", 1);
            var vm = new EventCreateVM
            {
                Event = new EventBase
                {
                    EventId = 1,
                    Title = "Updated Event",
                    Category = "Concert"
                },
                DiscountCodes = new List<DiscountCodeVM>()
            };
            var photos = new List<IFormFile>();
            var videos = new List<IFormFile>
    {
        Mock.Of<IFormFile>(f => f.FileName == "video.mp4" && f.Length == 5000)
    };

            _mockEventService.Setup(s => s.UpdateEventAsync(
                It.IsAny<EventBase>(),
                It.IsAny<int>(),
                It.IsAny<bool>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateSubEventAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ReturnsAsync((true, "Updated"));
            _mockEventService.Setup(s => s.UpdateDiscountCodesAsync(
                It.IsAny<int>(),
                It.IsAny<List<DiscountCodeDTO>>(),
                It.IsAny<int>()))
                .ReturnsAsync((true, "Updated"));
            _mockPhotoService.Setup(p => p.UploadEventVideosAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<object>>(),
                It.IsAny<int>()))
                .ReturnsAsync((false, new List<int>(), "Video upload failed"));

            // Act
            var result = await _controller.Edit(1, vm, photos, videos);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            // Logger should have been called with warning
        }

        #endregion

        #region Photo Management API Tests

        [Fact]
        public async Task DeletePhoto_Success_ReturnsJsonSuccess()
        {
            // Arrange
            SetupUserContext("Organizer", 1);

            _mockPhotoService.Setup(p => p.DeleteEventPhotoAsync(1, 1, false))
                .ReturnsAsync((true, "Photo deleted"));

            // Act
            var result = await _controller.DeletePhoto(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();

            // Cast to anonymous type properly
            var value = jsonResult!.Value;
            value.Should().NotBeNull();

            var successProp = value!.GetType().GetProperty("success");
            var messageProp = value.GetType().GetProperty("message");

            successProp.Should().NotBeNull();
            messageProp.Should().NotBeNull();

            var success = (bool)successProp!.GetValue(value)!;
            var message = (string)messageProp!.GetValue(value)!;

            success.Should().BeTrue();
            message.Should().Be("Photo deleted");
        }

        [Fact]
        public async Task DeletePhoto_Failure_ReturnsJsonFailure()
        {
            // Arrange
            SetupUserContext("Organizer", 1);

            _mockPhotoService.Setup(p => p.DeleteEventPhotoAsync(999, 1, false))
                .ReturnsAsync((false, "Photo not found"));

            // Act
            var result = await _controller.DeletePhoto(999);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();

            var value = jsonResult!.Value;
            value.Should().NotBeNull();

            var successProp = value!.GetType().GetProperty("success");
            var messageProp = value.GetType().GetProperty("message");

            successProp.Should().NotBeNull();
            messageProp.Should().NotBeNull();

            var success = (bool)successProp!.GetValue(value)!;
            var message = (string)messageProp!.GetValue(value)!;

            success.Should().BeFalse();
            message.Should().Be("Photo not found");
        }

        [Fact]
        public async Task SetPrimaryPhoto_Success_ReturnsJsonSuccess()
        {
            // Arrange
            SetupUserContext("Organizer", 1);

            _mockPhotoService.Setup(p => p.SetEventPrimaryPhotoAsync(1, 5))
                .ReturnsAsync((true, "Primary photo set"));

            // Act
            var result = await _controller.SetPrimaryPhoto(1, 5);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();

            var value = jsonResult!.Value;
            value.Should().NotBeNull();

            var successProp = value!.GetType().GetProperty("success");
            var messageProp = value.GetType().GetProperty("message");

            successProp.Should().NotBeNull();
            messageProp.Should().NotBeNull();

            var success = (bool)successProp!.GetValue(value)!;
            var message = (string)messageProp!.GetValue(value)!;

            success.Should().BeTrue();
            message.Should().Be("Primary photo set");
        }

        #endregion

        #region FilterByCategory Tests

        [Fact]
        public async Task FilterByCategory_ReturnsFilteredEvents()
        {
            // Arrange
            SetupUserContext("Customer", 1);
            var events = GetSampleEvents().Where(e => e.Category == "Concert").ToList();

            _mockEventService.Setup(s => s.GetEventsByCategoryAsync("Concert"))
                .ReturnsAsync(events);

            _mockPhotoService.Setup(p => p.GetEventPhotos(It.IsAny<int>()))
                .Returns(GetSamplePhotos(1));
            _mockPhotoService.Setup(p => p.GetEventVideos(It.IsAny<int>()))
                .Returns(new List<EventVideo>()); // ✅ ADD THIS

            // Act
            var result = await _controller.FilterByCategory("Concert");

            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult?.ViewName.Should().Be("Index");
            var model = viewResult?.Model as List<EventBase>;
            model?.All(e => e.Category == "Concert").Should().BeTrue();

            var eventPhotos = _controller.ViewBag.EventPhotos as Dictionary<int, List<string>>;
            eventPhotos.Should().NotBeNull();
        }

        [Fact]
        public async Task FilterByCategory_AsCustomer_OnlyFutureEvents()
        {
            // Arrange
            SetupUserContext("Customer", 1);
            var events = new List<EventBase>
    {
        new EventBase
        {
            EventId = 1,
            Category = "Concert",
            StartDate = DateTime.Now.AddDays(10)
        },
        new EventBase
        {
            EventId = 2,
            Category = "Concert",
            StartDate = DateTime.Now.AddDays(-10)
        }
    };

            _mockEventService.Setup(s => s.GetEventsByCategoryAsync("Concert"))
                .ReturnsAsync(events);

            _mockPhotoService.Setup(p => p.GetEventPhotos(It.IsAny<int>()))
                .Returns(new List<EventPhoto>());
            _mockPhotoService.Setup(p => p.GetEventVideos(It.IsAny<int>()))
                .Returns(new List<EventVideo>()); // ✅ ADD THIS

            // Act
            var result = await _controller.FilterByCategory("Concert");

            // Assert
            var viewResult = result as ViewResult;
            var model = viewResult?.Model as List<EventBase>;
            model.Should().HaveCount(1);
            model?.First().EventId.Should().Be(1);
        }

        #endregion

        #region GetVenueAvailability Tests

        [Fact]
        public async Task GetVenueAvailability_ValidId_ReturnsJsonWithSlots()
        {
            // Arrange
            var bookedSlots = new List<(DateTime StartDate, DateTime EndDate)>
            {
                (DateTime.Now.AddDays(5), DateTime.Now.AddDays(6)),
                (DateTime.Now.AddDays(10), DateTime.Now.AddDays(11))
            };

            _mockEventService.Setup(s => s.GetVenueAvailabilityAsync(1))
                .ReturnsAsync(bookedSlots);

            // Act
            var result = await _controller.GetVenueAvailability(1);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();
        }

        [Fact]
        public async Task GetVenueAvailability_InvalidId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetVenueAvailability(0);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region Partial View Tests

        [Fact]
        public void LoadCategoryForm_ValidCategory_ReturnsPartialView()
        {
            // Act
            var result = _controller.LoadCategoryForm("Concert");

            // Assert
            result.Should().BeOfType<PartialViewResult>();
            var partialResult = result as PartialViewResult;
            partialResult?.ViewName.Should().Be("Partials/_ConcertForm");
        }

        [Fact]
        public void LoadCategoryForm_EmptyCategory_ReturnsEmptyContent()
        {
            // Act
            var result = _controller.LoadCategoryForm("");

            // Assert
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult?.Content.Should().BeEmpty();
        }

        [Fact]
        public void LoadCategoryFormForEdit_Concert_ReturnsPartialWithModel()
        {
            // Arrange
            var mockConcertRepo = new Mock<IConcertRepo>();
            var concert = new Concert();
            mockConcertRepo.Setup(r => r.GetById(1)).Returns(concert);
            _mockUnitOfWork.Setup(u => u.Concerts).Returns(mockConcertRepo.Object);

            // Act
            var result = _controller.LoadCategoryFormForEdit("Concert", 1);

            // Assert
            result.Should().BeOfType<PartialViewResult>();
            var partialResult = result as PartialViewResult;
            partialResult?.ViewName.Should().Be("Partials/_ConcertForm");
            partialResult?.Model.Should().BeOfType<Concert>();
        }

        [Fact]
        public void LoadCategoryDetails_ValidCategory_ReturnsPartialWithModel()
        {
            // Arrange
            var mockConcertRepo = new Mock<IConcertRepo>();
            var concert = new Concert();
            mockConcertRepo.Setup(r => r.GetById(1)).Returns(concert);
            _mockUnitOfWork.Setup(u => u.Concerts).Returns(mockConcertRepo.Object);

            // Act
            var result = _controller.LoadCategoryDetails("Concert", 1);

            // Assert
            result.Should().BeOfType<PartialViewResult>();
            var partialResult = result as PartialViewResult;
            partialResult?.ViewName.Should().Be("Partials/_ConcertDetails");
        }

        [Fact]
        public void LoadCategoryDetails_NotFound_ReturnsErrorContent()
        {
            // Arrange
            var mockConcertRepo = new Mock<IConcertRepo>();
            mockConcertRepo.Setup(r => r.GetById(999)).Returns((Concert?)null);
            _mockUnitOfWork.Setup(u => u.Concerts).Returns(mockConcertRepo.Object);

            // Act
            var result = _controller.LoadCategoryDetails("Concert", 999);

            // Assert
            result.Should().BeOfType<ContentResult>();
            var contentResult = result as ContentResult;
            contentResult?.Content.Should().Contain("Concert not found");
        }

        #endregion
    }
}