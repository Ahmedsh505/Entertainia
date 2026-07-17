using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.PaymentModels;
using Entertania1.ViewModels.Book;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IPaymentService _paymentService;
        private readonly IRefundService _refundService;
        private readonly ICurrencyConversionService _currencyService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingController> _logger;
        private readonly ITransactionService _transactionService; // ✅ ADDED

        public BookingController(
            IBookingService bookingService,
            IPaymentService paymentService,
            IRefundService refundService,
            ICurrencyConversionService currencyService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<BookingController> logger,
            ITransactionService transactionService) // ✅ ADDED
        {
            _bookingService = bookingService;
            _paymentService = paymentService;
            _refundService = refundService;
            _currencyService = currencyService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _transactionService = transactionService; // ✅ ADDED
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        // =============== My Bookings ===============
        public async Task<IActionResult> MyBookings()
        {
            var currentUserId = GetCurrentUserId();
            var bookings = await _bookingService.GetUserBookingsAsync(currentUserId);
            return View(bookings);
        }

        // =============== Booking Details ===============
        [HttpGet]
        public async Task<IActionResult> Details(int bookingId)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null || (booking.UserId != currentUserId && !User.IsInRole("Admin")))
            {
                TempData["Error"] = "Booking not found or access denied.";
                return RedirectToAction(nameof(MyBookings));
            }

            var payment = _unitOfWork.Payments.GetByBookingId(bookingId);
            var refund = await _refundService.GetRefundByBookingIdAsync(bookingId);

            // ✅ NEW: Get tickets
            var tickets = _unitOfWork.Tickets.GetByBookingId(bookingId);

            var vm = new BookingDetailsViewModel
            {
                BookingId = booking.BookingId,
                BookingDate = booking.BookingDate,
                NumberOfTickets = booking.NumberOfTickets,
                Status = booking.Status,
                TicketTier = booking.TicketTier,

                EventId = booking.Event.EventId,
                EventTitle = booking.Event.Title,
                EventDescription = booking.Event.Description,
                EventCategory = booking.Event.Category,
                EventStartDate = booking.Event.StartDate,
                EventEndDate = booking.Event.EndDate,

                VenueName = booking.Event.Venue?.Name,
                VenueLocation = booking.Event.Venue?.Location,
                VenueLatitude = booking.Event.Venue?.Latitude,
                VenueLongitude = booking.Event.Venue?.Longitude,
                GoogleMapsUrl = booking.Event.Venue?.GoogleMapsUrl,

                PricePerTicket = booking.PricePerTicket,
                TotalPrice = booking.TotalPrice,
                Currency = booking.Currency,
                DiscountAmount = booking.DiscountAmount,
                TaxAmount = booking.TaxAmount,
                TotalWithTax = booking.TotalWithTax,

                DiscountCodeUsed = booking.DiscountCode?.Code,

                Payment = payment,
                Refund = refund,
                Tickets = tickets, // ✅ NEW
                UserName = booking.User?.UserName,
                UserEmail = booking.User?.Email
            };

            return View("BookingDetails", vm);
        }

        // =============== Book (GET) ===============
        [HttpGet]
        public async Task<IActionResult> Book(int eventId)
        {
            var currentUserId = GetCurrentUserId();

            var eventEntity = _unitOfWork.Events.GetById(eventId);
            if (eventEntity == null || !eventEntity.IsPublished)
            {
                TempData["Error"] = "Event not found or not available for booking.";
                return RedirectToAction("Index", "Events");
            }

            var venue = _unitOfWork.Venues.GetById(eventEntity.VenueId);
            if (venue == null)
            {
                TempData["Error"] = "Venue information not found.";
                return RedirectToAction("Index", "Events");
            }

            var ticketTiers = _unitOfWork.TicketTiers.GetByEvent(eventId);
            var tierOptions = ticketTiers
                .Where(t => t.IsActive)
                .Select(t => new TicketTierOption
                {
                    Tier = t.Tier,
                    Name = t.GetTierName(),
                    Price = t.GetPrice(eventEntity.Price),
                    AvailableSeats = t.AvailableSeats,
                    PriceMultiplier = t.PriceMultiplier
                })
                .ToList();

            decimal? estimatedEGP = null;
            if (eventEntity.Currency != Currency.EGP)
            {
                estimatedEGP = await _currencyService.ConvertToEGPAsync(eventEntity.Price, eventEntity.Currency);
            }

            var viewModel = new BookEventViewModel
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.Title,
                VenueName = venue.Name,
                VenueCapacity = venue.Capacity,
                UserId = currentUserId,
                BasePricePerTicket = eventEntity.Price,
                PricePerTicket = eventEntity.Price,
                Currency = eventEntity.Currency,
                AvailableTiers = tierOptions,
                EstimatedPriceInEGP = estimatedEGP
            };

            return View(viewModel);
        }

        // =============== Book (POST) ===============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookEventViewModel model)
        {
            var currentUserId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the form and try again.";
                return RedirectToAction(nameof(Book), new { eventId = model.EventId });
            }

            var result = await _bookingService.CreateBookingAsync(
                currentUserId,
                model.EventId,
                model.NumberOfTickets,
                model.SelectedTier,
                model.DiscountCode
            );

            if (!result.Success || result.Booking == null)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Book), new { eventId = model.EventId });
            }

            var payment = await _paymentService.InitiatePaymentAsync(result.Booking);
            var paymentUrl = await _paymentService.GeneratePaymentUrlAsync(payment);

            return Redirect(paymentUrl);
        }

        // =============== Retry Payment ===============
        [HttpGet]
        public async Task<IActionResult> RetryPayment(int bookingId)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null || booking.UserId != currentUserId)
            {
                TempData["Error"] = "Booking not found or access denied.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Status != "PendingPayment")
            {
                TempData["Error"] = "Only pending bookings can be retried for payment.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Event.StartDate <= DateTime.UtcNow)
            {
                TempData["Error"] = "Cannot retry payment for events that have already started.";
                return RedirectToAction(nameof(MyBookings));
            }

            try
            {
                _logger.LogInformation($"Retrying payment for booking {bookingId}");

                var existingPayment = _unitOfWork.Payments.GetByBookingId(bookingId);
                Payment payment;

                if (existingPayment != null)
                {
                    _logger.LogInformation($"Found existing payment {existingPayment.PaymentId}");
                    existingPayment.TransactionId = GenerateTransactionId();
                    existingPayment.Status = PaymentStatus.Pending;
                    existingPayment.CreatedAt = DateTime.UtcNow;
                    existingPayment.FailureReason = null;
                    existingPayment.FailedAt = null;
                    existingPayment.ProcessedAt = null;
                    existingPayment.CompletedAt = null;

                    _unitOfWork.Payments.Update(existingPayment);
                    _unitOfWork.Complete();
                    payment = existingPayment;
                }
                else
                {
                    booking.CalculateTax(14m);
                    _unitOfWork.Bookings.Update(booking);
                    _unitOfWork.Complete();
                    payment = await _paymentService.InitiatePaymentAsync(booking);
                }

                var paymentUrl = await _paymentService.GeneratePaymentUrlAsync(payment);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrying payment for booking {bookingId}");
                TempData["Error"] = $"Failed to retry payment: {ex.Message}";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        private string GenerateTransactionId()
        {
            return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        // =============== ✅ NEW: Direct Cancel for Pending Bookings ===============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPendingBooking(int bookingId)
        {
            var currentUserId = GetCurrentUserId();

            try
            {
                _logger.LogInformation($"User {currentUserId} attempting to cancel pending booking {bookingId}");

                var booking = await _bookingService.GetBookingByIdAsync(bookingId);

                if (booking == null || booking.UserId != currentUserId)
                {
                    TempData["Error"] = "Booking not found or access denied.";
                    return RedirectToAction(nameof(MyBookings));
                }

                // ✅ STRICT CHECK: Only allow cancellation of pending bookings
                if (booking.Status != "PendingPayment")
                {
                    TempData["Error"] = "Only pending bookings can be cancelled directly. For confirmed bookings, please request a refund.";
                    return RedirectToAction(nameof(MyBookings));
                }

                // ✅ STEP 1: Cancel the booking (restores seats and discount codes)
                var cancelResult = await _bookingService.CancelBookingAsync(bookingId, currentUserId);

                if (!cancelResult.Success)
                {
                    TempData["Error"] = cancelResult.Message;
                    return RedirectToAction(nameof(MyBookings));
                }

                // ✅ STEP 2: Log cancellation to Transactions table
                await _transactionService.LogCancellationAsync(
                    userId: currentUserId,
                    bookingId: bookingId,
                    reason: "User cancelled pending booking before payment"
                );

                _logger.LogInformation($"✅ Successfully cancelled pending booking {bookingId} and logged transaction");

                // ✅ STEP 3: Send cancellation email
                try
                {
                    var user = booking.User;
                    if (user != null && booking.Event != null)
                    {
                        await _emailService.SendBookingCancellationAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}",
                            booking.Event.Title,
                            "You cancelled your booking before completing payment. No charges were made."
                        );
                        _logger.LogInformation($"📧 Cancellation email sent to {user.Email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email");
                }

                TempData["Success"] = $"Booking cancelled successfully. {booking.NumberOfTickets} {booking.TicketTier} seat(s) are now available again.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling pending booking {bookingId}");
                TempData["Error"] = "An error occurred while cancelling the booking. Please try again.";
                return RedirectToAction(nameof(MyBookings));
            }
        }

        // =============== ✅ UPDATED: Cancel/Refund Route - Now Routes Based on Status ===============
        [HttpGet]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null || booking.UserId != currentUserId)
            {
                TempData["Error"] = "Booking not found or you don't have permission to cancel it.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Status == "Cancelled")
            {
                TempData["Error"] = "This booking is already cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }

            // ✅ CHECK: If booking is pending, show simple cancel confirmation
            if (booking.Status == "PendingPayment")
            {
                var pendingViewModel = new CancelBookingViewModel
                {
                    BookingId = booking.BookingId,
                    EventName = booking.Event?.Title ?? "Event",
                    BookingDate = booking.BookingDate,
                    NumberOfTickets = booking.NumberOfTickets,
                    UserId = currentUserId,
                    IsPendingPayment = true, // ✅ NEW FLAG
                    TotalAmount = booking.TotalWithTax,
                    Currency = booking.Currency
                };

                return View("CancelPending", pendingViewModel); // ✅ NEW VIEW
            }

            // ✅ CHECK: If booking is confirmed, route to refund request
            var existingRefund = await _refundService.GetRefundByBookingIdAsync(bookingId);
            if (existingRefund != null)
            {
                TempData["Error"] = "This booking has a refund request pending.";
                return RedirectToAction(nameof(MyBookings));
            }

            var viewModel = new CancelBookingViewModel
            {
                BookingId = booking.BookingId,
                EventName = booking.Event?.Title ?? "Event",
                BookingDate = booking.BookingDate,
                NumberOfTickets = booking.NumberOfTickets,
                UserId = currentUserId,
                IsPendingPayment = false,
                TotalAmount = booking.TotalWithTax,
                Currency = booking.Currency
            };

            return View(viewModel); // Regular refund request view
        }

        // =============== Cancel Confirmed (Refund Request) ===============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int bookingId, string? cancellationReason)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null || booking.UserId != currentUserId)
            {
                TempData["Error"] = "Booking not found or access denied.";
                return RedirectToAction(nameof(MyBookings));
            }

            var payment = _unitOfWork.Payments.GetByBookingId(bookingId);

            if (payment != null && payment.Status == PaymentStatus.Completed)
            {
                var refundReason = cancellationReason ?? "Customer requested cancellation";
                var refundResult = await _refundService.RequestRefundAsync(bookingId, currentUserId, refundReason);

                if (refundResult.Success)
                {
                    try
                    {
                        var user = booking.User;
                        if (user != null && booking.Event != null && refundResult.Refund != null)
                        {
                            await _emailService.SendRefundRequestedAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                booking.Event.Title,
                                refundResult.Refund.RefundAmount,
                                refundResult.Refund.Currency.ToString()
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to send refund requested email");
                    }

                    TempData["Success"] = "Refund request submitted successfully. Awaiting admin approval.";
                }
                else
                {
                    TempData["Error"] = refundResult.Message;
                }
            }
            else
            {
                var cancelResult = await _bookingService.CancelBookingAsync(bookingId, currentUserId);
                TempData[cancelResult.Success ? "Success" : "Error"] = cancelResult.Message;
            }

            return RedirectToAction(nameof(MyBookings));
        }

        // =============== Delete (Admin Only) ===============
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int bookingId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _bookingService.DeleteBookingAsync(bookingId, currentUserId, isAdmin: true);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Dashboard", "Admin");
        }

        // =============== Request Refund ===============
        [HttpGet]
        public async Task<IActionResult> RequestRefund(int bookingId)
        {
            var currentUserId = GetCurrentUserId();
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            if (booking == null || booking.UserId != currentUserId)
            {
                TempData["Error"] = "Booking not found or access denied.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (!string.Equals(booking.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only confirmed bookings can be refunded.";
                return RedirectToAction(nameof(MyBookings));
            }

            var payment = _unitOfWork.Payments.GetByBookingId(bookingId);
            if (payment == null || payment.Status != PaymentStatus.Completed)
            {
                TempData["Error"] = "No completed payment found for this booking.";
                return RedirectToAction(nameof(MyBookings));
            }

            var existingRefund = await _refundService.GetRefundByBookingIdAsync(bookingId);
            if (existingRefund != null)
            {
                TempData["Error"] = "A refund request already exists for this booking.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (booking.Event.StartDate <= DateTime.UtcNow)
            {
                TempData["Error"] = "Cannot request refund for events that have already started.";
                return RedirectToAction(nameof(MyBookings));
            }

            ViewBag.Booking = booking;
            ViewBag.Payment = payment;
            ViewBag.RefundAmount = Refund.CalculateRefundAmount(payment.TotalAmountWithTax, 80m);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefundConfirmed(int bookingId, string reason)
        {
            var currentUserId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Please provide a reason for the refund request.";
                return RedirectToAction(nameof(RequestRefund), new { bookingId });
            }

            var result = await _refundService.RequestRefundAsync(bookingId, currentUserId, reason);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(MyBookings));
        }

        // =============== Apply Discount Code (AJAX) ===============
        [HttpPost]
        public async Task<IActionResult> ApplyDiscountCode([FromBody] DiscountCodeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DiscountCode))
            {
                return Json(new { success = false, message = "Please enter a discount code" });
            }

            var discount = _unitOfWork.DiscountCodes.GetValidCodeForEvent(request.DiscountCode, request.EventId);

            if (discount == null || !discount.IsValid())
            {
                return Json(new { success = false, message = "Invalid or expired discount code" });
            }

            var eventEntity = _unitOfWork.Events.GetById(request.EventId);
            if (eventEntity == null)
            {
                return Json(new { success = false, message = "Event not found" });
            }

            var sampleAmount = eventEntity.Price;
            var discountAmount = discount.CalculateDiscount(sampleAmount);

            return Json(new
            {
                success = true,
                message = $"Discount code applied! You save {discountAmount:N2} {eventEntity.Currency}",
                discountAmount = discountAmount,
                discountPercentage = discount.DiscountType == DiscountType.Percentage ? discount.DiscountValue : 0
            });
        }
    }

    public class DiscountCodeRequest
    {
        public int EventId { get; set; }
        public string DiscountCode { get; set; } = string.Empty;
    }
}