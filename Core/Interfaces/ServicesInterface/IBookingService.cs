using Core.Enums;
using Core.Models;
using Core.Models.BookingModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service interface for Booking business logic
    /// </summary>
    public interface IBookingService
    {
        // ===== BOOKING CRUD =====
        Task<Booking?> GetBookingByIdAsync(int bookingId);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<List<Booking>> GetEventBookingsAsync(int eventId);
        Task<List<Booking>> GetAllBookingsAsync();

        // ===== CREATE BOOKING =====
        Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(
            int userId,
            int eventId,
            int numberOfTickets,
            TicketTier selectedTier,
            string? discountCode = null);

        // ===== UPDATE BOOKING =====
        Task<(bool Success, string Message)> UpdateBookingAsync(
            int bookingId,
            int userId,
            int newTicketCount,
            TicketTier newTier);

        // ===== CANCEL BOOKING =====
        Task<(bool Success, string Message)> CancelBookingAsync(int bookingId, int userId);

        // ===== DELETE BOOKING =====
        Task<(bool Success, string Message)> DeleteBookingAsync(int bookingId, int userId, bool isAdmin = false);

        // ===== VALIDATION =====
        Task<(bool IsValid, string ErrorMessage)> ValidateBookingAsync(
            int eventId,
            int numberOfTickets,
            TicketTier tier,
            int? excludeBookingId = null);

        Task<bool> ValidateTicketAvailabilityAsync(
            int eventId,
            TicketTier tier,
            int requestedTickets,
            int? excludeBookingId = null);

        // ===== HELPER METHODS =====
        Task<int> GetAvailableSeatsForTierAsync(int eventId, TicketTier tier, int? excludeBookingId = null);
        Task<decimal> CalculateBookingPriceAsync(int eventId, TicketTier tier, int numberOfTickets, string? discountCode = null);

        // ===== DISCOUNT CODE =====
        Task<(bool IsValid, DiscountCode? Code, decimal DiscountAmount)> ValidateAndApplyDiscountAsync(
            string discountCode,
            int eventId,
            decimal totalPrice);

        // ===== SEAT MANAGEMENT =====
        Task RestoreTierSeatsAsync(int eventId, TicketTier tier, int seatsToRestore);
        Task DecrementTierSeatsAsync(int eventId, TicketTier tier, int seatsToDecrement);

        // ===== ✅ NEW: ABANDONED BOOKING CLEANUP =====
        /// <summary>
        /// Cancels bookings stuck in PendingPayment status for too long
        /// </summary>
        /// <param name="timeoutMinutes">Minutes to wait before considering a booking abandoned (default: 30)</param>
        /// <returns>Number of bookings cancelled</returns>
        Task<int> CancelAbandonedBookingsAsync(int timeoutMinutes = 30);
    }
}