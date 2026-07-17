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
    public class DiscountCodeTests
    {
        [Fact]
        public void IsValid_ShouldReturnFalse_WhenExpired()
        {
            var code = new DiscountCode
            {
                IsActive = true,
                ValidUntil = DateTime.UtcNow.AddDays(-1)
            };

            code.IsValid().Should().BeFalse();
        }

        [Fact]
        public void CalculateDiscount_ShouldApplyPercentageCorrectly()
        {
            var code = new DiscountCode
            {
                IsActive = true,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 20m
            };

            var discount = code.CalculateDiscount(100m);
            discount.Should().Be(20m);
        }

        [Fact]
        public void CalculateDiscount_ShouldNotExceedOriginalPrice_WhenFixedAmount()
        {
            var code = new DiscountCode
            {
                IsActive = true,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 200m
            };

            code.CalculateDiscount(150m).Should().Be(150m);
        }
    }

}
