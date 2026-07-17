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
    public class UserTests
    {
        [Fact]
        public void CanLogin_ShouldReturnTrue_WhenActiveAndNotDeleted()
        {
            var user = new User
            {
                AccountStatus = AccountStatus.Active,
                IsDeleted = false,
                DeletionType = DeletionType.None
            };

            user.CanLogin().Should().BeTrue();
        }

        [Fact]
        public void CanLogin_ShouldReturnFalse_WhenDeleted()
        {
            var user = new User
            {
                AccountStatus = AccountStatus.Active,
                IsDeleted = true
            };

            user.CanLogin().Should().BeFalse();
        }

        [Fact]
        public void IsEmailBanned_ShouldReturnTrue_WhenAdminBanned()
        {
            var user = new User
            {
                IsDeleted = true,
                DeletionType = DeletionType.AdminBanned
            };

            user.IsEmailBanned().Should().BeTrue();
        }

        [Fact]
        public void DefaultValues_ShouldBeSetProperly()
        {
            var user = new User();
            user.AccountStatus.Should().Be(AccountStatus.PendingApproval);
            user.IsDeleted.Should().BeFalse();
            user.CreatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        }
    }

}
