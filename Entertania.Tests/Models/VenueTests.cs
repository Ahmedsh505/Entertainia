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
    public class VenueTests
    {
        [Fact]
        public void GetDisplayName_ShouldReturnDeletedPrefix_WhenDeleted()
        {
            var venue = new Venue { IsDeleted = true, Name = "Arena" };
            venue.GetDisplayName().Should().Contain("[Deleted]");
        }

        [Fact]
        public void CanBeUsedForNewEvents_ShouldReturnTrue_WhenPublishedAndNotDeleted()
        {
            var venue = new Venue
            {
                ApprovalStatus = ApprovalStatus.Approved,
                IsDeleted = false
            };

            venue.CanBeUsedForNewEvents().Should().BeTrue();
        }

        [Fact]
        public void HasScheduleConflict_ShouldReturnTrue_WhenOverlapExists()
        {
            var existing = new EventBase
            {
                EventId = 1,
                ApprovalStatus = ApprovalStatus.Approved,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddHours(2)
            };

            var venue = new Venue();
            venue.Events.Add(existing);

            venue.HasScheduleConflict(DateTime.UtcNow.AddMinutes(30), DateTime.UtcNow.AddHours(1))
                  .Should().BeTrue();
        }
    }

}
