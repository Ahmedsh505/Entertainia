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
    public class PaymentTests
    {
        [Fact]
        public void CalculateTax_ShouldComputeAccurately()
        {
            var payment = new Payment { Amount = 100m };
            payment.CalculateTax();
            payment.TaxAmount.Should().Be(14m);
            payment.TotalAmountWithTax.Should().Be(114m);
        }

        [Fact]
        public void DefaultValues_ShouldBeInitializedCorrectly()
        {
            var payment = new Payment();
            payment.Status.Should().Be(PaymentStatus.Pending);
            payment.PaymentGateway.Should().Be("Paymob");
        }
    }

}
