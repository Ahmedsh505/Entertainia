using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.BookingModels;
using Core.Models.EventModels;
using Core.Models;
using Core.Enums;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entertania.Tests.Services
{
    public class BookingServiceTests
    {
        // =====================================================
        // SETUP & HELPERS
        // =====================================================

        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<BookingService>> _mockLogger;
        private readonly Mock<IBookingRepo> _mockBookingRepo;
        private readonly Mock<IEventRepo> _mockEventRepo;
        private readonly Mock<IEventTicketTierRepo> _mockTicketTierRepo;
        private readonly Mock<IDiscountCodeRepo> _mockDiscountCodeRepo;
        private readonly Mock<IEgyptTimeService> _mockgyptTimeService;
        // ✅ FIX 1: Define the Service Mock, not the Repository Mock
        private readonly Mock<IPaymentHistoryService> _mockPaymentHistoryService;

        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            // Initialize mocks
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<BookingService>>();
            _mockBookingRepo = new Mock<IBookingRepo>();
            _mockEventRepo = new Mock<IEventRepo>();
            _mockTicketTierRepo = new Mock<IEventTicketTierRepo>();
            _mockDiscountCodeRepo = new Mock<IDiscountCodeRepo>();
            _mockgyptTimeService = new Mock<IEgyptTimeService>();

            // ✅ FIX 2: Initialize the Service Mock
            _mockPaymentHistoryService = new Mock<IPaymentHistoryService>();

            // ✅ FIX 3: Setup the LogAsync method to return a completed Task
            // This prevents NullReferenceExceptions when the service awaits this method
            _mockPaymentHistoryService
                .Setup(x => x.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<PaymentStatus>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup UnitOfWork to return mocked repositories
            _mockUnitOfWork.Setup(uow => uow.Bookings).Returns(_mockBookingRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.Events).Returns(_mockEventRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.TicketTiers).Returns(_mockTicketTierRepo.Object);
            _mockUnitOfWork.Setup(uow => uow.DiscountCodes).Returns(_mockDiscountCodeRepo.Object);

            // Create service instance
            _bookingService = new BookingService(
                _mockUnitOfWork.Object,
                _mockEmailService.Object,
                _mockLogger.Object,
                _mockPaymentHistoryService.Object, // ✅ FIX 4: Pass the Service Object
                _mockgyptTimeService.Object
            );
        }

        // =====================================================
        // TEST DATA BUILDERS
        // =====================================================

        private EventBase CreateTestEvent(
            int eventId = 1,
            decimal price = 100m,
            bool isPublished = true,
            DateTime? startDate = null)
        {
            return new EventBase
            {
                EventId = eventId,
                Title = "Test Concert",
                Description = "A test event",
                Price = price,
                Currency = Currency.EGP,
                StartDate = startDate ?? DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(8),
                VenueId = 1,
                OrganizerId = 1,
                ApprovalStatus = isPublished ? ApprovalStatus.Approved : ApprovalStatus.Pending,
                Category = "Concert",
                Venue = new Venue
                {
                    VenueId = 1,
                    Name = "Test Venue",
                    Capacity = 1000
                }
            };
        }

        private EventTicketTier CreateTestTicketTier(
            int eventId = 1,
            TicketTier tier = TicketTier.Regular,
            int availableSeats = 800,
            decimal multiplier = 1.0m)
        {
            return new EventTicketTier
            {
                EventTicketTierId = 1,
                EventId = eventId,
                Tier = tier,
                PriceMultiplier = multiplier,
                AvailableSeats = availableSeats,
                IsActive = true,
                CapacityPercentage = 0.8m
            };
        }

        private DiscountCode CreateTestDiscountCode(
            int eventId = 1,
            string code = "SAVE20",
            DiscountType type = DiscountType.Percentage,
            decimal value = 20m,
            bool isActive = true)
        {
            return new DiscountCode
            {
                DiscountCodeId = 1,
                Code = code,
                EventId = eventId,
                DiscountType = type,
                DiscountValue = value,
                IsActive = isActive,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(30),
                MaxUsageCount = 100,
                CurrentUsageCount = 0,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        // =====================================================
        // BOOKING CREATION TESTS
        // =====================================================

        [Fact]
        public async Task CreateBookingAsync_ValidBooking_ReturnsSuccessWithBooking()
        {
            // Arrange
            int userId = 1;
            int eventId = 1;
            int numberOfTickets = 2;
            TicketTier selectedTier = TicketTier.Regular;

            var testEvent = CreateTestEvent();
            var testTier = CreateTestTicketTier(availableSeats: 800);

            _mockEventRepo.Setup(r => r.GetById(eventId)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(eventId, selectedTier))
                .Returns(testTier);
            _mockBookingRepo.Setup(r => r.Add(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);
            _mockTicketTierRepo.Setup(r => r.UpdateAvailableSeats(It.IsAny<int>(), It.IsAny<int>()));

            // Act
            var result = await _bookingService.CreateBookingAsync(
                userId, eventId, numberOfTickets, selectedTier);

            // Assert
            Assert.True(result.Success, "Booking creation should succeed");
            Assert.NotNull(result.Booking);
            Assert.Equal("PendingPayment", result.Booking.Status);
            Assert.Equal(numberOfTickets, result.Booking.NumberOfTickets);
            Assert.Equal(100m, result.Booking.PricePerTicket);
            Assert.Equal(200m, result.Booking.TotalPrice);
            Assert.Equal(TicketTier.Regular, result.Booking.TicketTier);

            _mockBookingRepo.Verify(r => r.Add(It.IsAny<Booking>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Complete(), Times.Exactly(2));
        }

        [Fact]
        public async Task CreateBookingAsync_WithValidDiscountCode_AppliesDiscount()
        {
            // Arrange
            int userId = 1;
            int eventId = 1;
            int numberOfTickets = 2;
            string discountCode = "SAVE20";

            var testEvent = CreateTestEvent();
            var testTier = CreateTestTicketTier();
            var testDiscount = CreateTestDiscountCode(code: discountCode, value: 20m);

            _mockEventRepo.Setup(r => r.GetById(eventId)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(eventId, TicketTier.Regular))
                .Returns(testTier);
            _mockDiscountCodeRepo.Setup(r => r.GetValidCodeForEvent(discountCode, eventId))
                .Returns(testDiscount);
            _mockDiscountCodeRepo.Setup(r => r.IncrementUsageCount(It.IsAny<int>()));
            _mockBookingRepo.Setup(r => r.Add(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);

            // Act
            var result = await _bookingService.CreateBookingAsync(
                userId, eventId, numberOfTickets, TicketTier.Regular, discountCode);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Booking);
            Assert.Equal(200m, result.Booking.TotalPrice);
            Assert.Equal(40m, result.Booking.DiscountAmount); // 20% of 200
            Assert.Equal(testDiscount.DiscountCodeId, result.Booking.DiscountCodeId);

            _mockDiscountCodeRepo.Verify(r => r.IncrementUsageCount(testDiscount.DiscountCodeId), Times.Once);
        }

        [Fact]
        public async Task CreateBookingAsync_VIPTier_CalculatesCorrectPrice()
        {
            // Arrange
            int eventId = 1;
            var testEvent = CreateTestEvent(price: 100m);
            var vipTier = CreateTestTicketTier(
                tier: TicketTier.VIP,
                availableSeats: 50,
                multiplier: 2.5m
            );

            _mockEventRepo.Setup(r => r.GetById(eventId)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(eventId, TicketTier.VIP))
                .Returns(vipTier);
            _mockBookingRepo.Setup(r => r.Add(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);

            // Act
            var result = await _bookingService.CreateBookingAsync(
                1, eventId, 1, TicketTier.VIP);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(250m, result.Booking!.PricePerTicket); // 100 * 2.5
            Assert.Equal(250m, result.Booking.TotalPrice);
            Assert.Equal(TicketTier.VIP, result.Booking.TicketTier);
        }

        [Fact]
        public async Task CreateBookingAsync_CalculatesTaxCorrectly()
        {
            // Arrange
            int eventId = 1;
            var testEvent = CreateTestEvent(price: 100m);
            var testTier = CreateTestTicketTier();

            _mockEventRepo.Setup(r => r.GetById(eventId)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(eventId, TicketTier.Regular))
                .Returns(testTier);
            _mockBookingRepo.Setup(r => r.Add(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);

            // Act
            var result = await _bookingService.CreateBookingAsync(
                1, eventId, 2, TicketTier.Regular);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Booking);
            Assert.Equal(200m, result.Booking.TotalPrice);
            Assert.Equal(28m, result.Booking.TaxAmount); // 14% of 200
            Assert.Equal(228m, result.Booking.TotalWithTax);
        }

        // =====================================================
        // VALIDATION TESTS
        // =====================================================

        [Fact]
        public async Task CreateBookingAsync_WithZeroTickets_ReturnsFail()
        {
            int numberOfTickets = 0;
            var result = await _bookingService.CreateBookingAsync(
                1, 1, numberOfTickets, TicketTier.Regular);

            Assert.False(result.Success);
            Assert.Contains("must be at least 1", result.Message);
            Assert.Null(result.Booking);
        }

        [Fact]
        public async Task CreateBookingAsync_EventNotFound_ReturnsFail()
        {
            int eventId = 999;
            _mockEventRepo.Setup(r => r.GetById(eventId)).Returns((EventBase?)null);

            var result = await _bookingService.CreateBookingAsync(
                1, eventId, 2, TicketTier.Regular);

            Assert.False(result.Success);
            Assert.Equal("Event not found", result.Message);
            Assert.Null(result.Booking);
        }

        [Fact]
        public async Task CreateBookingAsync_EventNotPublished_ReturnsFail()
        {
            var unpublishedEvent = CreateTestEvent(isPublished: false);
            _mockEventRepo.Setup(r => r.GetById(1)).Returns(unpublishedEvent);

            var result = await _bookingService.CreateBookingAsync(
                1, 1, 2, TicketTier.Regular);

            Assert.False(result.Success);
            Assert.Contains("not available for booking", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_InsufficientSeats_ReturnsFail()
        {
            var testEvent = CreateTestEvent();
            var testTier = CreateTestTicketTier(availableSeats: 5);

            _mockEventRepo.Setup(r => r.GetById(1)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(1, TicketTier.Regular))
                .Returns(testTier);

            var result = await _bookingService.CreateBookingAsync(
                1, 1, 10, TicketTier.Regular);

            Assert.False(result.Success);
            Assert.Contains("Only 5", result.Message);
            Assert.Contains("tickets available", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_TierNotAvailable_ReturnsFail()
        {
            var testEvent = CreateTestEvent();
            _mockEventRepo.Setup(r => r.GetById(1)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(1, TicketTier.VIP))
                .Returns((EventTicketTier?)null);

            var result = await _bookingService.CreateBookingAsync(
                1, 1, 2, TicketTier.VIP);

            Assert.False(result.Success);
            Assert.Equal("VIP tier is not available for this event", result.Message);
        }

        [Fact]
        public async Task CreateBookingAsync_InvalidDiscountCode_IgnoresDiscount()
        {
            var testEvent = CreateTestEvent();
            var testTier = CreateTestTicketTier();
            string invalidCode = "INVALID";

            _mockEventRepo.Setup(r => r.GetById(1)).Returns(testEvent);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(1, TicketTier.Regular))
                .Returns(testTier);
            _mockDiscountCodeRepo.Setup(r => r.GetValidCodeForEvent(invalidCode, 1))
                .Returns((DiscountCode?)null);
            _mockBookingRepo.Setup(r => r.Add(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);

            var result = await _bookingService.CreateBookingAsync(
                1, 1, 2, TicketTier.Regular, invalidCode);

            Assert.True(result.Success);
            Assert.Equal(0m, result.Booking!.DiscountAmount);
            Assert.Null(result.Booking.DiscountCodeId);
        }

        // =====================================================
        // BOOKING CANCELLATION TESTS
        // =====================================================

        [Fact]
        public async Task CancelBookingAsync_ValidBooking_RestoresSeatsAndSucceeds()
        {
            // Arrange
            int bookingId = 1;
            int userId = 1;
            var booking = new Booking
            {
                BookingId = bookingId,
                UserId = userId,
                EventId = 1,
                NumberOfTickets = 2,
                TicketTier = TicketTier.Regular,
                Status = "PendingPayment", // Changed to PendingPayment to trigger log flow
                User = new User { Id = userId, Email = "test@example.com", FirstName = "Test" },
                Event = new EventBase { Title = "Test Event" }
            };

            var testTier = CreateTestTicketTier(eventId: 1, tier: TicketTier.Regular);

            _mockBookingRepo.Setup(r => r.GetById(bookingId)).Returns(booking);
            _mockTicketTierRepo.Setup(r => r.GetByEventAndTier(1, TicketTier.Regular))
                .Returns(testTier);
            _mockTicketTierRepo.Setup(r => r.RestoreAvailableSeats(It.IsAny<int>(), It.IsAny<int>()));
            _mockBookingRepo.Setup(r => r.Update(It.IsAny<Booking>()));
            _mockUnitOfWork.Setup(uow => uow.Complete()).Returns(1);
            _mockEmailService.Setup(e => e.SendBookingCancellationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("cancelled successfully", result.Message);
            Assert.Equal("Cancelled", booking.Status);

            // Verify seats were restored
            _mockTicketTierRepo.Verify(
                r => r.RestoreAvailableSeats(testTier.EventTicketTierId, 2),
                Times.Once);

            // Verify Payment History Log was called
            _mockPaymentHistoryService.Verify(
                x => x.LogAsync(userId, bookingId, 0, PaymentStatus.Failed, It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task CancelBookingAsync_UnauthorizedUser_ReturnsFail()
        {
            int bookingId = 1;
            int bookingOwnerId = 1;
            int unauthorizedUserId = 2;

            var booking = new Booking
            {
                BookingId = bookingId,
                UserId = bookingOwnerId,
                Status = "Confirmed"
            };

            _mockBookingRepo.Setup(r => r.GetById(bookingId)).Returns(booking);

            var result = await _bookingService.CancelBookingAsync(bookingId, unauthorizedUserId);

            Assert.False(result.Success);
            Assert.Contains("don't have permission", result.Message);
            _mockBookingRepo.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task CancelBookingAsync_AlreadyCancelled_ReturnsFail()
        {
            var booking = new Booking
            {
                BookingId = 1,
                UserId = 1,
                Status = "Cancelled"
            };

            _mockBookingRepo.Setup(r => r.GetById(1)).Returns(booking);

            var result = await _bookingService.CancelBookingAsync(1, 1);

            Assert.False(result.Success);
            Assert.Contains("already cancelled", result.Message);
        }

        // =====================================================
        // DISCOUNT CODE TESTS
        // =====================================================

        [Fact]
        public async Task ValidateAndApplyDiscountAsync_ValidCode_ReturnsDiscount()
        {
            var discount = CreateTestDiscountCode(
                type: DiscountType.Percentage,
                value: 20m
            );
            decimal totalPrice = 200m;

            _mockDiscountCodeRepo.Setup(r => r.GetValidCodeForEvent("SAVE20", 1))
                .Returns(discount);

            var result = await _bookingService.ValidateAndApplyDiscountAsync(
                "SAVE20", 1, totalPrice);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Code);
            Assert.Equal(40m, result.DiscountAmount);
        }

        [Fact]
        public async Task ValidateAndApplyDiscountAsync_FixedAmountDiscount_CalculatesCorrectly()
        {
            var discount = CreateTestDiscountCode(
                type: DiscountType.FixedAmount,
                value: 50m
            );

            _mockDiscountCodeRepo.Setup(r => r.GetValidCodeForEvent("FIXED50", 1))
                .Returns(discount);

            var result = await _bookingService.ValidateAndApplyDiscountAsync(
                "FIXED50", 1, 200m);

            Assert.True(result.IsValid);
            Assert.Equal(50m, result.DiscountAmount);
        }

        [Fact]
        public async Task GetBookingByIdAsync_ValidId_ReturnsBooking()
        {
            var booking = new Booking { BookingId = 1, UserId = 1 };
            _mockBookingRepo.Setup(r => r.GetById(1)).Returns(booking);

            var result = await _bookingService.GetBookingByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.BookingId);
        }

        [Fact]
        public async Task GetBookingByIdAsync_InvalidId_ReturnsNull()
        {
            _mockBookingRepo.Setup(r => r.GetById(999)).Returns((Booking?)null);

            var result = await _bookingService.GetBookingByIdAsync(999);

            Assert.Null(result);
        }
    }
}