using Xunit;
using FluentAssertions;
using Core.Models;
using Core.Models.BookingModels;
using Core.Models.EventModels;
using Core.Models.PaymentModels;
using Core.Enums;
using System;


namespace Entertania.Tests.Models
{
    public class EventBaseTests
    {
        [Fact]
        public void IsPublished_ShouldBeTrue_WhenApproved()
        {
            var ev = new EventBase { ApprovalStatus = ApprovalStatus.Approved };
            ev.IsPublished.Should().BeTrue();
        }

        [Fact]
        public void DefaultValues_ShouldInitializeProperly()
        {
            var ev = new EventBase();
            ev.TicketTiers.Should().NotBeNull();
            ev.DiscountCodes.Should().NotBeNull();
            ev.Bookings.Should().NotBeNull();
            ev.Currency.Should().Be(Currency.EGP);
        }
    }

}
