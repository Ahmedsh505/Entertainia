using Core.Enums;
using Core.Models.PaymentModels;
using Core.Models.TicketModels;
using System;

namespace Entertania1.ViewModels.Book
{
    /// <summary>
    /// ViewModel for displaying comprehensive booking details
    /// ✅ UPDATED: Now handles "Pending Refund" status
    /// </summary>
    public class BookingDetailsViewModel
    {
        // Booking Info
        public int BookingId { get; set; }
        public DateTime BookingDate { get; set; }
        public int NumberOfTickets { get; set; }
        public string? Status { get; set; }
        public TicketTier TicketTier { get; set; }
        public List<Ticket> Tickets { get; set; }
        public bool HasTickets => Tickets != null && Tickets.Count > 0;


        // Event Info
        public int EventId { get; set; }
        public string? EventTitle { get; set; }
        public string? EventDescription { get; set; }
        public string? EventCategory { get; set; }
        public DateTime EventStartDate { get; set; }
        public DateTime EventEndDate { get; set; }

        // Venue Info
        public string? VenueName { get; set; }
        public string? VenueLocation { get; set; }
        public double? VenueLatitude { get; set; }
        public double? VenueLongitude { get; set; }
        public string? GoogleMapsUrl { get; set; }

        // Pricing Info
        public decimal PricePerTicket { get; set; }
        public decimal TotalPrice { get; set; }
        public Currency Currency { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalWithTax { get; set; }

        // Discount Info
        public string? DiscountCodeUsed { get; set; }
        public bool HasDiscount => DiscountAmount > 0;

        // Payment Info
        public Payment? Payment { get; set; }
        public bool IsPaid => Payment?.Status == PaymentStatus.Completed;

        // Refund Info
        public Refund? Refund { get; set; }
        public bool HasRefund => Refund != null;
        public bool IsRefunded => Status == "Refunded" || Refund?.Status == PaymentStatus.Refunded;
        public bool IsRefundPending => Status == "Pending Refund" && Refund?.Status == PaymentStatus.Pending;

        // User Info
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }

        // Helper methods
        public string GetTierName()
        {
            return TicketTier switch
            {
                Core.Enums.TicketTier.VIP => "VIP",
                Core.Enums.TicketTier.Special => "Special",
                Core.Enums.TicketTier.Regular => "Regular",
                _ => "Unknown"
            };
        }

        public bool CanCancel()
        {
            return Status != "Cancelled" &&
                   Status != "Refunded" &&
                   Status != "Pending Refund" &&
                   Status != "Refund Rejected" &&
                   EventStartDate > DateTime.UtcNow;
        }

        public bool CanRequestRefund()
        {
            return (Status == "Confirmed" || Status == "Refund Rejected") &&
                   IsPaid &&
                   EventStartDate > DateTime.UtcNow;
        }

        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                "Confirmed" => "badge bg-success",
                "Cancelled" => "badge bg-danger",
                "PendingPayment" => "badge bg-warning text-dark",
                "Pending Refund" => "badge bg-info text-dark",
                "Refund Rejected" => "badge bg-danger", // ✅ NEW
                "Refunded" => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }

        public string GetPaymentStatusBadgeClass()
        {
            if (Payment == null) return "badge bg-secondary";

            return Payment.Status switch
            {
                PaymentStatus.Completed => "badge bg-success",
                PaymentStatus.Failed => "badge bg-danger",
                PaymentStatus.Pending => "badge bg-warning",
                PaymentStatus.Processing => "badge bg-info",
                PaymentStatus.Refunded => "badge bg-dark",
                _ => "badge bg-secondary"
            };
        }

        public string GetRefundStatusBadgeClass()
        {
            if (Refund == null) return "badge bg-secondary";

            return Refund.Status switch
            {
                PaymentStatus.Pending => "badge bg-warning text-dark",
                PaymentStatus.Processing => "badge bg-info",
                PaymentStatus.Refunded => "badge bg-success",
                PaymentStatus.Failed => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }
    }
}