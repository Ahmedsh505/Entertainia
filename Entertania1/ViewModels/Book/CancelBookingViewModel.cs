using Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Book
{
    /// <summary>
    /// ViewModel for both pending booking cancellation and confirmed booking refund requests
    /// </summary>
    public class CancelBookingViewModel
    {
        public int BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }

        public int NumberOfTickets { get; set; }

        /// <summary>
        /// Flag to determine if this is a pending booking (direct cancel)
        /// or confirmed booking (refund request)
        /// </summary>
        public bool IsPendingPayment { get; set; }

        /// <summary>
        /// Total amount for display
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency
        /// </summary>
        public Currency Currency { get; set; }

        /// <summary>
        /// Cancellation/Refund reason (optional for pending, can be required for refunds)
        /// </summary>
        [StringLength(1000)]
        public string? CancellationReason { get; set; }

        // Helper properties for UI
        public string ActionText => IsPendingPayment ? "Cancel Booking" : "Request Refund";

        public string ConfirmationMessage => IsPendingPayment
            ? "Are you sure you want to cancel this booking? No charges have been made yet."
            : "Are you sure you want to request a refund? This will be subject to admin approval and you will receive 80% of the paid amount.";

        public string ButtonText => IsPendingPayment ? "Yes, Cancel Booking" : "Yes, Request Refund";

        public string ButtonClass => IsPendingPayment ? "btn btn-danger" : "btn btn-warning";
    }
}