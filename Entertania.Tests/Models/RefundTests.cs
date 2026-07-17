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
    public class RefundTests
    {
        [Fact]
        public void CalculateRefund_ShouldComputeCorrectly()
        {
            var refund = new Refund { OriginalAmount = 100m, RefundPercentage = 50m };
            refund.CalculateRefund();
            refund.RefundAmount.Should().Be(50m);
        }

        [Fact]
        public void StaticRefundCalculation_ShouldThrow_WhenInvalidPercentage()
        {
            Action act = () => Refund.CalculateRefundAmount(100m, 150m);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void CanBeProcessed_ShouldReturnTrue_ForPendingFutureEvent()
        {
            var refund = new Refund
            {
                Status = PaymentStatus.Pending,
                Booking = new Core.Models.BookingModels.Booking
                {
                    Event = new Core.Models.EventModels.EventBase
                    {
                        StartDate = DateTime.UtcNow.AddDays(2)
                    }
                }
            };

            refund.CanBeProcessed().Should().BeTrue();
        }
    }

}
