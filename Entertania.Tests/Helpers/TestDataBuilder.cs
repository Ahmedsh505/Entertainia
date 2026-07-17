using Core.Enums;
using Core.Models;
using Core.Models.BookingModels;
using Core.Models.EventModels;
using System;
using System.Collections.Generic;

namespace Entertania.Tests.Helpers
{
    /// <summary>
    /// Reusable test data builders and utilities
    /// Use these across all test classes for consistency
    /// </summary>
    public static class TestDataBuilder
    {
        // =====================================================
        // EVENT BUILDERS
        // =====================================================

        public static EventBase BuildEvent(Action<EventBase> configure = null)
        {
            var eventBase = new EventBase
            {
                EventId = 1,
                Title = "Test Event",
                Description = "A test event for unit testing",
                Price = 100m,
                Currency = Currency.EGP,
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(8),
                VenueId = 1,
                OrganizerId = 1,
                ApprovalStatus = ApprovalStatus.Approved,
                Category = "Concert",
                CreatedAt = DateTime.UtcNow,
                Venue = BuildVenue()
            };

            configure?.Invoke(eventBase);
            return eventBase;
        }

        public static EventBase BuildUnpublishedEvent()
        {
            return BuildEvent(e => e.ApprovalStatus = ApprovalStatus.Pending);
        }

        public static EventBase BuildPastEvent()
        {
            return BuildEvent(e =>
            {
                e.StartDate = DateTime.UtcNow.AddDays(-2);
                e.EndDate = DateTime.UtcNow.AddDays(-1);
            });
        }

        // =====================================================
        // VENUE BUILDERS
        // =====================================================

        public static Venue BuildVenue(Action<Venue> configure = null)
        {
            var venue = new Venue
            {
                VenueId = 1,
                Name = "Test Venue",
                Location = "Cairo, Egypt",
                Capacity = 1000,
                ApprovalStatus = ApprovalStatus.Approved
            };

            configure?.Invoke(venue);
            return venue;
        }

        // =====================================================
        // USER BUILDERS
        // =====================================================

        public static User BuildUser(Action<User> configure = null)
        {
            var user = new User
            {
                Id = 1,  // Only use Id property
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                UserName = "testuser",
                Role = "Customer",
                AccountStatus = AccountStatus.Active,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            configure?.Invoke(user);
            return user;
        }

        public static User BuildOrganizer()
        {
            return BuildUser(u =>
            {
                u.Id = 2;  // Only use Id property
                u.Role = "Organizer";
                u.Email = "organizer@example.com";
            });
        }

        public static User BuildAdmin()
        {
            return BuildUser(u =>
            {
                u.Id = 3;  // Only use Id property
                u.Role = "Admin";
                u.Email = "admin@example.com";
            });
        }

        // =====================================================
        // BOOKING BUILDERS
        // =====================================================

        public static Booking BuildBooking(Action<Booking> configure = null)
        {
            var booking = new Booking
            {
                BookingId = 1,
                UserId = 1,
                EventId = 1,
                BookingDate = DateTime.UtcNow,
                NumberOfTickets = 2,
                Status = "Confirmed",
                PricePerTicket = 100m,
                TotalPrice = 200m,
                Currency = Currency.EGP,
                TicketTier = TicketTier.Regular,
                TaxAmount = 28m,
                TotalWithTax = 228m
            };

            configure?.Invoke(booking);
            return booking;
        }

        public static Booking BuildPendingBooking()
        {
            return BuildBooking(b => b.Status = "PendingPayment");
        }

        public static Booking BuildCancelledBooking()
        {
            return BuildBooking(b => b.Status = "Cancelled");
        }

        // =====================================================
        // TICKET TIER BUILDERS
        // =====================================================

        public static EventTicketTier BuildTicketTier(Action<EventTicketTier> configure = null)
        {
            var tier = new EventTicketTier
            {
                EventTicketTierId = 1,
                EventId = 1,
                Tier = TicketTier.Regular,
                PriceMultiplier = 1.0m,
                CapacityPercentage = 0.8m,
                AvailableSeats = 800,
                IsActive = true
            };

            configure?.Invoke(tier);
            return tier;
        }

        public static EventTicketTier BuildVIPTier()
        {
            return BuildTicketTier(t =>
            {
                t.Tier = TicketTier.VIP;
                t.PriceMultiplier = 2.5m;
                t.CapacityPercentage = 0.05m;
                t.AvailableSeats = 50;
            });
        }

        public static EventTicketTier BuildSpecialTier()
        {
            return BuildTicketTier(t =>
            {
                t.Tier = TicketTier.Special;
                t.PriceMultiplier = 1.5m;
                t.CapacityPercentage = 0.15m;
                t.AvailableSeats = 150;
            });
        }

        // =====================================================
        // DISCOUNT CODE BUILDERS
        // =====================================================

        public static DiscountCode BuildDiscountCode(Action<DiscountCode> configure = null)
        {
            var discount = new DiscountCode
            {
                DiscountCodeId = 1,
                Code = "TEST20",
                EventId = 1,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 20m,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(30),
                MaxUsageCount = 100,
                CurrentUsageCount = 0,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            };

            configure?.Invoke(discount);
            return discount;
        }

        public static DiscountCode BuildFixedAmountDiscount(decimal amount = 50m)
        {
            return BuildDiscountCode(d =>
            {
                d.DiscountType = DiscountType.FixedAmount;
                d.DiscountValue = amount;
                d.Code = $"FIXED{amount}";
            });
        }

        public static DiscountCode BuildExpiredDiscount()
        {
            return BuildDiscountCode(d =>
            {
                d.ValidUntil = DateTime.UtcNow.AddDays(-1);
                d.Code = "EXPIRED";
            });
        }

        public static DiscountCode BuildMaxedOutDiscount()
        {
            return BuildDiscountCode(d =>
            {
                d.MaxUsageCount = 10;
                d.CurrentUsageCount = 10;
                d.Code = "MAXEDOUT";
            });
        }

        // =====================================================
        // COLLECTION BUILDERS
        // =====================================================

        public static List<EventTicketTier> BuildStandardTierSet(int eventId = 1)
        {
            return new List<EventTicketTier>
            {
                BuildTicketTier(t => { t.EventId = eventId; t.Tier = TicketTier.Regular; }),
                BuildSpecialTier(),
                BuildVIPTier()
            };
        }

        public static List<Booking> BuildBookingList(int count, int eventId = 1)
        {
            var bookings = new List<Booking>();
            for (int i = 1; i <= count; i++)
            {
                bookings.Add(BuildBooking(b =>
                {
                    b.BookingId = i;
                    b.EventId = eventId;
                    b.UserId = i;
                }));
            }
            return bookings;
        }
    }

    /// <summary>
    /// Constants for testing
    /// </summary>
    public static class TestConstants
    {
        public const int DEFAULT_USER_ID = 1;
        public const int DEFAULT_EVENT_ID = 1;
        public const int DEFAULT_VENUE_ID = 1;
        public const int DEFAULT_ORGANIZER_ID = 2;
        public const int DEFAULT_ADMIN_ID = 3;

        public const decimal DEFAULT_EVENT_PRICE = 100m;
        public const int DEFAULT_VENUE_CAPACITY = 1000;
        public const decimal DEFAULT_TAX_PERCENTAGE = 14m;

        public const string DEFAULT_USER_EMAIL = "test@example.com";
        public const string DEFAULT_ORGANIZER_EMAIL = "organizer@example.com";

        public const int ABANDONED_BOOKING_TIMEOUT_MINUTES = 30;
    }

    /// <summary>
    /// Custom assertions for cleaner test code
    /// </summary>
    public static class CustomAssertions
    {
        public static void AssertBookingIsValid(Booking booking)
        {
            if (booking == null)
                throw new ArgumentNullException(nameof(booking));

            if (booking.BookingId <= 0)
                throw new Exception("Booking must have a valid ID");

            if (booking.NumberOfTickets <= 0)
                throw new Exception("Booking must have at least 1 ticket");

            if (booking.TotalPrice <= 0)
                throw new Exception("Booking must have a valid price");

            if (string.IsNullOrEmpty(booking.Status))
                throw new Exception("Booking must have a status");
        }

        public static void AssertPriceCalculation(
            decimal basePrice,
            int tickets,
            decimal multiplier,
            decimal expectedTotal)
        {
            var calculated = basePrice * tickets * multiplier;
            if (Math.Abs(calculated - expectedTotal) > 0.01m)
            {
                throw new Exception(
                    $"Price calculation mismatch. Expected: {expectedTotal}, " +
                    $"Calculated: {calculated}");
            }
        }

        public static void AssertDiscountApplied(
            decimal originalPrice,
            decimal discountAmount,
            decimal finalPrice)
        {
            var expected = originalPrice - discountAmount;
            if (Math.Abs(expected - finalPrice) > 0.01m)
            {
                throw new Exception(
                    $"Discount not applied correctly. Original: {originalPrice}, " +
                    $"Discount: {discountAmount}, Expected: {expected}, Actual: {finalPrice}");
            }
        }
    }
}