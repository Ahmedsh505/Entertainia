using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.BookingModels;
using Core.Models.EventModels;
using Core.Models.PaymentModels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Entertania.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private readonly HttpClient _httpClient;
        private readonly PaymentService _service;
        private readonly Mock<IPaymentHistoryService> _paymentHistoryServiceMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IEgyptTimeService> _mockgyptTimeService;


        public PaymentServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _configMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<PaymentService>>();
            _paymentHistoryServiceMock = new Mock<IPaymentHistoryService>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _mockgyptTimeService = new Mock<IEgyptTimeService>();


            // Configuration setup
            _configMock.Setup(c => c["Paymob:ApiKey"]).Returns("FAKE_KEY");
            _configMock.Setup(c => c["Paymob:IntegrationId"]).Returns("12345");
            _configMock.Setup(c => c["Paymob:HmacSecret"]).Returns("secret123");
            _configMock.Setup(c => c["Paymob:IframeId"]).Returns("99999");
            _configMock.Setup(c => c["Paymob:PaymentUrl"]).Returns("https://accept.paymob.com/api/acceptance/iframes");

            // HttpClient mock (returns fake Paymob response)
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"token\":\"FAKE_TOKEN\",\"id\":1}")
                });

            _httpClient = new HttpClient(handlerMock.Object);

            _service = new PaymentService(
                _unitOfWorkMock.Object,
                _configMock.Object,
                _httpClient,
                _loggerMock.Object,
                _paymentHistoryServiceMock.Object,
                _serviceProviderMock.Object,
                _mockgyptTimeService.Object

            );
        }

        // ───────────────────────────── CORE TESTS ─────────────────────────────

        [Fact]
        public async Task InitiatePaymentAsync_ShouldCreatePaymentSuccessfully()
        {
            var booking = new Booking
            {
                BookingId = 1,
                TotalPrice = 100m,
                DiscountAmount = 10m,
                Currency = Currency.EGP
            };

            var paymentRepo = new Mock<IPaymentRepo>();
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var payment = await _service.InitiatePaymentAsync(booking);

            payment.Should().NotBeNull();
            payment.Amount.Should().Be(90m);
            payment.Status.Should().Be(PaymentStatus.Pending);
            paymentRepo.Verify(r => r.Add(It.IsAny<Payment>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.Complete(), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ShouldUpdateStatusToProcessing()
        {
            var payment = new Payment { PaymentId = 1, Status = PaymentStatus.Pending, BookingId = 1 };
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.GetById(1)).Returns(payment);
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var bookingRepo = new Mock<IBookingRepo>();
            bookingRepo.Setup(r => r.GetById(1)).Returns(new Booking { BookingId = 1, UserId = 1 });
            _unitOfWorkMock.Setup(u => u.Bookings).Returns(bookingRepo.Object);

            // FIX: mock logging to return a valid Task
            _paymentHistoryServiceMock
                .Setup(s => s.LogAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<decimal>(),
                    It.IsAny<PaymentStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var result = await _service.ProcessPaymentAsync(1, "TX123");

            result.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Processing);
            payment.TransactionId.Should().Be("TX123");

            paymentRepo.Verify(r => r.Update(payment), Times.Once);
        }


        [Fact]
        public async Task CompletePaymentAsync_ShouldMarkPaymentAndBookingCompleted()
        {
            var booking = new Booking { Status = "Pending" };
            var payment = new Payment { PaymentId = 1, Booking = booking, Status = PaymentStatus.Processing };
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.GetById(1)).Returns(payment);
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var bookingRepo = new Mock<IBookingRepo>();
            _unitOfWorkMock.Setup(u => u.Bookings).Returns(bookingRepo.Object);

            var result = await _service.CompletePaymentAsync(1);

            result.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Completed);
            booking.Status.Should().Be("Confirmed");
            paymentRepo.Verify(r => r.Update(payment), Times.Once);
            bookingRepo.Verify(r => r.Update(booking), Times.Once);
        }

        [Fact]
        public async Task FailPaymentAsync_ShouldMarkPaymentFailed_AndCancelBooking()
        {
            var booking = new Booking { Status = "Confirmed" };
            var payment = new Payment { PaymentId = 1, Booking = booking };
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.GetById(1)).Returns(payment);
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var bookingRepo = new Mock<IBookingRepo>();
            _unitOfWorkMock.Setup(u => u.Bookings).Returns(bookingRepo.Object);

            var result = await _service.FailPaymentAsync(1, "Card Declined");

            result.Should().BeTrue();
            payment.Status.Should().Be(PaymentStatus.Failed);
            booking.Status.Should().Be("Cancelled");
            payment.FailureReason.Should().Be("Card Declined");
        }

        [Fact]
        public async Task GetPaymentByIdAsync_ShouldReturnPayment()
        {
            var payment = new Payment { PaymentId = 10 };
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.GetById(10)).Returns(payment);
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var result = await _service.GetPaymentByIdAsync(10);
            result.Should().Be(payment);
        }

        [Fact]
        public async Task VerifyPaymentCallbackAsync_ShouldReturnTrue_ForValidSignature()
        {
            var callback = new Dictionary<string, string>
            {
                { "hmac", "" },
                { "amount_cents", "10000" },
                { "created_at", "2025-01-01T00:00:00" },
                { "currency", "EGP" },
                { "id", "1" },
                { "integration_id", "12345" },
                { "success", "true" }
            };

            var concat = string.Concat(callback["amount_cents"], callback["created_at"], callback["currency"],
                "", "", callback["id"], callback["integration_id"],
                "", "", "", "", "", "", "", "", "", "", "", callback["success"]);

            using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes("secret123"));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concat));
            callback["hmac"] = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            var result = await _service.VerifyPaymentCallbackAsync(callback);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task InitiatePaymentAsync_ShouldThrow_AndLogError_WhenExceptionOccurs()
        {
            var booking = new Booking { BookingId = 99, TotalPrice = 10, Currency = Currency.EGP };
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.Add(It.IsAny<Payment>())).Throws(new Exception("DB error"));
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            Func<Task> act = async () => await _service.InitiatePaymentAsync(booking);
            await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
        }

        // ───────────────────────────── EXTENDED TESTS ─────────────────────────────

        [Fact]
        public async Task GeneratePaymentUrlAsync_ShouldReturnValidIframeUrl()
        {
            var user = new User { FirstName = "Ahmed", Email = "a@b.com", PhoneNumber = "0123456" };
            var booking = new Booking { BookingId = 1, User = user, Event = new EventBase { Title = "Concert" }, NumberOfTickets = 2 };
            var payment = new Payment { BookingId = 1, TotalAmountWithTax = 100m, Currency = Currency.EGP, TransactionId = "TX123" };

            var bookingRepo = new Mock<IBookingRepo>();
            bookingRepo.Setup(r => r.GetById(1)).Returns(booking);
            _unitOfWorkMock.Setup(u => u.Bookings).Returns(bookingRepo.Object);

            var result = await _service.GeneratePaymentUrlAsync(payment);

            result.Should().Contain("https://accept.paymob.com/api/acceptance/iframes");
            result.Should().Contain("payment_token=FAKE_TOKEN");
        }

        [Fact]
        public async Task GetTotalRevenueForOrganizerAsync_ShouldReturnCorrectValue()
        {
            var paymentRepo = new Mock<IPaymentRepo>();
            paymentRepo.Setup(r => r.GetTotalRevenueForOrganizer(5)).Returns(2500m);
            _unitOfWorkMock.Setup(u => u.Payments).Returns(paymentRepo.Object);

            var result = await _service.GetTotalRevenueForOrganizerAsync(5);
            result.Should().Be(2500m);
        }

        [Fact]
        public async Task GetOrganizerFinancialsAsync_ShouldCalculateTotalsCorrectly()
        {
            var bookings = new List<Booking>
            {
                new Booking { Event = new EventBase { OrganizerId = 10 }, Status = "Confirmed", TotalWithTax = 110m, TaxAmount = 10m, DiscountAmount = 5m },
                new Booking { Event = new EventBase { OrganizerId = 10 }, Status = "Confirmed", TotalWithTax = 220m, TaxAmount = 20m, DiscountAmount = 10m }
            }.AsQueryable();

            var bookingRepo = new Mock<IBookingRepo>();
            bookingRepo.Setup(r => r.GetAll()).Returns(bookings.ToList());
            _unitOfWorkMock.Setup(u => u.Bookings).Returns(bookingRepo.Object);

            var (totalRevenue, totalTax, totalDiscount) = await _service.GetOrganizerFinancialsAsync(10);

            totalRevenue.Should().Be(330m);
            totalTax.Should().Be(30m);
            totalDiscount.Should().Be(15m);
        }

        [Fact]
        public async Task VerifyPaymentCallbackAsync_ShouldReturnFalse_WhenHmacInvalid()
        {
            var callback = new Dictionary<string, string>
            {
                { "hmac", "wrong" },
                { "amount_cents", "10000" },
                { "created_at", "2025-01-01T00:00:00" },
                { "currency", "EGP" },
                { "id", "1" },
                { "integration_id", "12345" },
                { "success", "true" }
            };

            var result = await _service.VerifyPaymentCallbackAsync(callback);
            result.Should().BeFalse();
        }
    }
}
