using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models;
using Core.Models.BookingModels;
using Microsoft.Extensions.Logging;
using Core.Interfaces.RepositoriesInterface;

namespace Service.Services
{
    /// <summary>
    /// ✅ COMPLETE: Logs cancellations to payment history properly
    /// </summary>
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;
        private readonly IPaymentHistoryService _paymentHistoryService; // ✅ ADDED
        private readonly IEgyptTimeService _timeService;

        public BookingService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<BookingService> logger,
            IPaymentHistoryService paymentHistoryService,
            IEgyptTimeService timeService) // ✅ ADDED
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _paymentHistoryService = paymentHistoryService; // ✅ ADDED
            _timeService = timeService;
        }

        #region Get Bookings

        public async Task<Booking?> GetBookingByIdAsync(int bookingId)
        {
            return await Task.Run(() => _unitOfWork.Bookings.GetById(bookingId));
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await Task.Run(() => _unitOfWork.Bookings.GetByUser(userId));
        }

        public async Task<List<Booking>> GetEventBookingsAsync(int eventId)
        {
            return await Task.Run(() => _unitOfWork.Bookings.GetByEvent(eventId));
        }

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await Task.Run(() => _unitOfWork.Bookings.GetAll());
        }

        #endregion

        #region Create Booking

        public async Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(
            int userId,
            int eventId,
            int numberOfTickets,
            TicketTier selectedTier,
            string? discountCode = null)
        {
            try
            {
                _logger.LogInformation($"Creating booking for user {userId}, event {eventId}");

                if (numberOfTickets < 1)
                {
                    return (false, "Number of tickets must be at least 1", null);
                }

                var eventEntity = _unitOfWork.Events.GetById(eventId);
                if (eventEntity == null)
                {
                    return (false, "Event not found", null);
                }

                if (!eventEntity.IsPublished)
                {
                    return (false, "Event is not available for booking", null);
                }

                var validation = await ValidateBookingAsync(eventId, numberOfTickets, selectedTier);
                if (!validation.IsValid)
                {
                    return (false, validation.ErrorMessage, null);
                }

                var tier = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, selectedTier);
                if (tier == null)
                {
                    return (false, $"{selectedTier} tier not available for this event", null);
                }

                var pricePerTicket = tier.GetPrice(eventEntity.Price);
                var totalPrice = pricePerTicket * numberOfTickets;

                decimal discountAmount = 0;
                int? discountCodeId = null;

                if (!string.IsNullOrEmpty(discountCode))
                {
                    var discountResult = await ValidateAndApplyDiscountAsync(discountCode, eventId, totalPrice);
                    if (discountResult.IsValid && discountResult.Code != null)
                    {
                        discountAmount = discountResult.DiscountAmount;
                        discountCodeId = discountResult.Code.DiscountCodeId;
                        _unitOfWork.DiscountCodes.IncrementUsageCount(discountResult.Code.DiscountCodeId);
                    }
                }

                var booking = new Booking
                {
                    UserId = userId,
                    EventId = eventId,
                    BookingDate = DateTime.Now,
                    NumberOfTickets = numberOfTickets,
                    Status = "PendingPayment",
                    PricePerTicket = pricePerTicket,
                    TotalPrice = totalPrice,
                    Currency = eventEntity.Currency,
                    TicketTier = selectedTier,
                    DiscountCodeId = discountCodeId,
                    DiscountAmount = discountAmount
                };

                booking.CalculateTax(14m);

                _unitOfWork.Bookings.Add(booking);
                _unitOfWork.Complete();

                await DecrementTierSeatsAsync(eventId, selectedTier, numberOfTickets);

                _logger.LogInformation($"Booking created with PendingPayment status: ID {booking.BookingId}");

                return (true, "Booking created. Please complete payment to confirm.", booking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating booking for user {userId}, event {eventId}");
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        public async Task<int> CancelAbandonedBookingsAsync(int timeoutMinutes = 30)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
                var abandonedBookings = _unitOfWork.Bookings.GetAll()
                    .Where(b => b.Status == "PendingPayment" && b.BookingDate < cutoffTime)
                    .ToList();

                int cancelledCount = 0;

                foreach (var booking in abandonedBookings)
                {
                    await RestoreTierSeatsAsync(booking.EventId, booking.TicketTier, booking.NumberOfTickets);

                    if (booking.DiscountCodeId.HasValue)
                    {
                        var discount = _unitOfWork.DiscountCodes.GetById(booking.DiscountCodeId.Value);
                        if (discount != null && discount.CurrentUsageCount > 0)
                        {
                            discount.CurrentUsageCount--;
                            _unitOfWork.DiscountCodes.Update(discount);
                        }
                    }

                    // ✅ LOG automatic cancellation to payment history
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        booking.BookingId,
                        0, // $0 for abandoned booking
                        PaymentStatus.Failed,
                        method: "Booking Cancelled - Payment Not Completed (Timeout)",
                        transactionId: $"ABANDONED-{booking.BookingId}"
                    );

                    booking.Status = "Cancelled";
                    _unitOfWork.Bookings.Update(booking);

                    cancelledCount++;
                }

                if (cancelledCount > 0)
                {
                    _unitOfWork.Complete();
                    _logger.LogInformation($"Cancelled {cancelledCount} abandoned bookings and logged to payment history");
                }

                return cancelledCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling abandoned bookings");
                return 0;
            }
        }

        #endregion

        #region Update Booking

        public async Task<(bool Success, string Message)> UpdateBookingAsync(
            int bookingId,
            int userId,
            int newTicketCount,
            TicketTier newTier)
        {
            try
            {
                _logger.LogInformation($"Updating booking {bookingId}");

                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found");
                }

                if (booking.UserId != userId)
                {
                    return (false, "You don't have permission to update this booking");
                }

                if (booking.Status == "Cancelled")
                {
                    return (false, "Cannot update a cancelled booking");
                }

                if (newTicketCount < 1)
                {
                    return (false, "Number of tickets must be at least 1");
                }

                var eventEntity = _unitOfWork.Events.GetById(booking.EventId);
                if (eventEntity == null)
                {
                    return (false, "Event not found");
                }

                var oldTier = booking.TicketTier;
                var oldTicketCount = booking.NumberOfTickets;

                var newTierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(booking.EventId, newTier);
                if (newTierEntity == null)
                {
                    return (false, $"{newTier} tier not available");
                }

                var availableSeats = newTierEntity.AvailableSeats;
                if (oldTier == newTier)
                {
                    availableSeats += oldTicketCount;
                }

                if (availableSeats < newTicketCount)
                {
                    return (false, $"Only {availableSeats} {newTier} tickets available");
                }

                await RestoreTierSeatsAsync(booking.EventId, oldTier, oldTicketCount);
                await DecrementTierSeatsAsync(booking.EventId, newTier, newTicketCount);

                var newPricePerTicket = newTierEntity.GetPrice(eventEntity.Price);
                var newTotalPrice = newPricePerTicket * newTicketCount;

                decimal newDiscountAmount = 0;
                if (booking.DiscountCodeId.HasValue)
                {
                    var discount = _unitOfWork.DiscountCodes.GetById(booking.DiscountCodeId.Value);
                    if (discount != null && discount.IsValid())
                    {
                        newDiscountAmount = discount.CalculateDiscount(newTotalPrice);
                    }
                }

                booking.NumberOfTickets = newTicketCount;
                booking.TicketTier = newTier;
                booking.PricePerTicket = newPricePerTicket;
                booking.TotalPrice = newTotalPrice;
                booking.DiscountAmount = newDiscountAmount;
                booking.BookingDate = DateTime.UtcNow;

                booking.CalculateTax(14m);

                _unitOfWork.Bookings.Update(booking);
                _unitOfWork.Complete();

                _logger.LogInformation($"Booking {bookingId} updated successfully");

                var changeMessage = oldTier == newTier
                    ? $"Changed from {oldTicketCount} to {newTicketCount} {newTier} ticket(s)"
                    : $"Changed from {oldTicketCount} {oldTier} to {newTicketCount} {newTier} ticket(s)";

                return (true, $"Booking updated successfully! {changeMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating booking {bookingId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Cancel Booking

        public async Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int userId)
        {
            try
            {
                _logger.LogInformation($"Cancelling booking {bookingId}");

                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found");
                }

                if (booking.UserId != userId)
                {
                    return (false, "You don't have permission to cancel this booking");
                }

                if (booking.Status == "Cancelled")
                {
                    return (false, "This booking is already cancelled");
                }

                // ✅ NEW: Log cancellation for PendingPayment bookings
                if (booking.Status == "PendingPayment")
                {
                    await _paymentHistoryService.LogAsync(
                        userId,
                        bookingId,
                        0, // $0 for unpaid cancellation
                        PaymentStatus.Failed,
                        method: "Booking Cancelled - Payment Not Completed",
                        transactionId: $"CANCEL-{bookingId}"
                    );
                    _logger.LogInformation($"✅ Logged cancellation to payment history for booking {bookingId}");
                }

                await RestoreTierSeatsAsync(booking.EventId, booking.TicketTier, booking.NumberOfTickets);

                if (booking.DiscountCodeId.HasValue)
                {
                    var discount = _unitOfWork.DiscountCodes.GetById(booking.DiscountCodeId.Value);
                    if (discount != null && discount.CurrentUsageCount > 0)
                    {
                        discount.CurrentUsageCount--;
                        _unitOfWork.DiscountCodes.Update(discount);
                    }
                }

                booking.Status = "Cancelled";
                _unitOfWork.Bookings.Update(booking);
                _unitOfWork.Complete();

                _logger.LogInformation($"Booking {bookingId} cancelled successfully");

                try
                {
                    var user = booking.User;
                    if (user != null && booking.Event != null)
                    {
                        await _emailService.SendBookingCancellationAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}",
                            booking.Event.Title ?? "Event",
                            "Your booking was cancelled before payment completion."
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email");
                }

                return (true, $"Booking cancelled successfully. {booking.NumberOfTickets} {booking.TicketTier} seat(s) are now available.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling booking {bookingId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Delete Booking

        public async Task<(bool Success, string Message)> DeleteBookingAsync(int bookingId, int userId, bool isAdmin = false)
        {
            try
            {
                _logger.LogInformation($"Deleting booking {bookingId}");

                var booking = await GetBookingByIdAsync(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found");
                }

                if (!isAdmin && booking.UserId != userId)
                {
                    return (false, "You don't have permission to delete this booking");
                }

                // ✅ Optionally log deletion (admin action)
                if (isAdmin)
                {
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        bookingId,
                        0,
                        PaymentStatus.Failed,
                        method: "Booking Deleted by Admin",
                        transactionId: $"ADMIN-DELETE-{bookingId}"
                    );
                }

                await RestoreTierSeatsAsync(booking.EventId, booking.TicketTier, booking.NumberOfTickets);

                if (booking.DiscountCodeId.HasValue)
                {
                    var discount = _unitOfWork.DiscountCodes.GetById(booking.DiscountCodeId.Value);
                    if (discount != null && discount.CurrentUsageCount > 0)
                    {
                        discount.CurrentUsageCount--;
                        _unitOfWork.DiscountCodes.Update(discount);
                    }
                }

                _unitOfWork.Bookings.Delete(booking);
                _unitOfWork.Complete();

                _logger.LogInformation($"Booking {bookingId} deleted successfully");

                return (true, $"Booking deleted successfully. {booking.NumberOfTickets} {booking.TicketTier} seat(s) are now available.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting booking {bookingId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Validation

        public async Task<(bool IsValid, string ErrorMessage)> ValidateBookingAsync(
            int eventId,
            int numberOfTickets,
            TicketTier tier,
            int? excludeBookingId = null)
        {
            var eventEntity = _unitOfWork.Events.GetById(eventId);
            if (eventEntity == null)
            {
                return (false, "Event not found");
            }

            if (!eventEntity.IsPublished)
            {
                return (false, "Event is not available for booking");
            }

            if (eventEntity.StartDate <= DateTime.UtcNow)
            {
                return (false, "Cannot book tickets for an event that has already started");
            }

            var tierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, tier);
            if (tierEntity == null || !tierEntity.IsActive)
            {
                return (false, $"{tier} tier is not available for this event");
            }

            if (!await ValidateTicketAvailabilityAsync(eventId, tier, numberOfTickets, excludeBookingId))
            {
                var availableSeats = await GetAvailableSeatsForTierAsync(eventId, tier, excludeBookingId);
                return (false, $"Only {availableSeats} {tier} tickets available");
            }

            return (true, string.Empty);
        }

        public async Task<bool> ValidateTicketAvailabilityAsync(
            int eventId,
            TicketTier tier,
            int requestedTickets,
            int? excludeBookingId = null)
        {
            var availableSeats = await GetAvailableSeatsForTierAsync(eventId, tier, excludeBookingId);
            return availableSeats >= requestedTickets;
        }

        #endregion

        #region Helper Methods

        public async Task<int> GetAvailableSeatsForTierAsync(int eventId, TicketTier tier, int? excludeBookingId = null)
        {
            return await Task.Run(() =>
            {
                var tierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, tier);
                if (tierEntity == null) return 0;

                var availableSeats = tierEntity.AvailableSeats;

                if (excludeBookingId.HasValue)
                {
                    var excludedBooking = _unitOfWork.Bookings.GetById(excludeBookingId.Value);
                    if (excludedBooking != null && excludedBooking.TicketTier == tier)
                    {
                        availableSeats += excludedBooking.NumberOfTickets;
                    }
                }

                return availableSeats;
            });
        }

        public async Task<decimal> CalculateBookingPriceAsync(
            int eventId,
            TicketTier tier,
            int numberOfTickets,
            string? discountCode = null)
        {
            var eventEntity = _unitOfWork.Events.GetById(eventId);
            if (eventEntity == null) return 0;

            var tierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, tier);
            if (tierEntity == null) return 0;

            var pricePerTicket = tierEntity.GetPrice(eventEntity.Price);
            var totalPrice = pricePerTicket * numberOfTickets;

            if (!string.IsNullOrEmpty(discountCode))
            {
                var discountResult = await ValidateAndApplyDiscountAsync(discountCode, eventId, totalPrice);
                if (discountResult.IsValid)
                {
                    totalPrice -= discountResult.DiscountAmount;
                }
            }

            return totalPrice;
        }

        public async Task<(bool IsValid, DiscountCode? Code, decimal DiscountAmount)> ValidateAndApplyDiscountAsync(
            string discountCode,
            int eventId,
            decimal totalPrice)
        {
            return await Task.Run(() =>
            {
                var discount = _unitOfWork.DiscountCodes.GetValidCodeForEvent(discountCode, eventId);

                if (discount == null || !discount.IsValid())
                {
                    return (false, null, 0m);
                }

                var discountAmount = discount.CalculateDiscount(totalPrice);
                return (true, discount, discountAmount);
            });
        }

        public async Task RestoreTierSeatsAsync(int eventId, TicketTier tier, int seatsToRestore)
        {
            await Task.Run(() =>
            {
                var tierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, tier);
                if (tierEntity != null)
                {
                    _unitOfWork.TicketTiers.RestoreAvailableSeats(tierEntity.EventTicketTierId, seatsToRestore);
                    _unitOfWork.Complete();
                }
            });
        }

        public async Task DecrementTierSeatsAsync(int eventId, TicketTier tier, int seatsToDecrement)
        {
            await Task.Run(() =>
            {
                var tierEntity = _unitOfWork.TicketTiers.GetByEventAndTier(eventId, tier);
                if (tierEntity != null)
                {
                    _unitOfWork.TicketTiers.UpdateAvailableSeats(tierEntity.EventTicketTierId, seatsToDecrement);
                    _unitOfWork.Complete();
                }
            });
        }

        #endregion
    }
}