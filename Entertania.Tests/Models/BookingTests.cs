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
    public class BookingTests
    {
        [Fact]
        public void CalculateTax_ShouldComputeCorrectValues()
        {
            var booking = new Booking
            {
                TotalPrice = 1000m,
                DiscountAmount = 100m
            };

            booking.CalculateTax();
            booking.TaxAmount.Should().BeApproximately(126m, 0.1m);
            booking.TotalWithTax.Should().BeApproximately(1026m, 0.1m);
        }

        [Fact]
        public void Booking_ShouldHaveDefaultValues()
        {
            var booking = new Booking();
            booking.BookingDate.Should().BeBefore(DateTime.Now.AddSeconds(1));
            booking.TicketTier.Should().Be(TicketTier.Regular);
        }
    }

}
