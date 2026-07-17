using Core.Models;
using Core.Enums;
using Core.Models.EventModels;
using Core.Models.AuthModels;
using Core.Models.PaymentModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service interface for admin-specific business logic
    /// NO UI DEPENDENCIES - Returns domain models only!
    /// </summary>
    public interface IAdminService
    {
        // ===== DASHBOARD DATA =====
        /// <summary>
        /// Get dashboard statistics and pending items
        /// </summary>
        Task<AdminDashboardData> GetDashboardDataAsync();

        // ===== ORGANIZER MANAGEMENT =====
        Task<(bool Success, string Message)> ApproveOrganizerAsync(int organizerId);
        Task<(bool Success, string Message)> RejectOrganizerAsync(int organizerId);

        // ===== VENUE MANAGEMENT =====
        Task<(bool Success, string Message)> ApproveVenueAsync(int venueId, int adminId);
        Task<(bool Success, string Message)> RejectVenueAsync(int venueId, int adminId);

        // ===== EVENT MANAGEMENT =====
        Task<(bool Success, string Message)> ApproveEventAsync(int eventId, int adminId);
        Task<(bool Success, string Message)> RejectEventAsync(int eventId, int adminId);

        // ===== REFUND MANAGEMENT =====
        Task<(bool Success, string Message)> ApproveRefundAsync(int refundId, int adminId);
        Task<(bool Success, string Message)> RejectRefundAsync(int refundId, int adminId, string reason);

        // ===== DOCUMENT MANAGEMENT =====
        Task<List<VerificationDoc>> GetOrganizerDocumentsAsync(int organizerId);
        Task<(bool Success, VerificationDoc? Document, string ErrorMessage)> GetVerificationDocumentAsync(int docId);
    }

    /// <summary>
    /// Domain model for dashboard data - NO ViewModels here!
    /// </summary>
    public class AdminDashboardData
    {
        // Pending Items
        public List<User> PendingOrganizers { get; set; } = new();
        public List<Venue> PendingVenues { get; set; } = new();
        public List<EventBase> PendingEvents { get; set; } = new();
        public List<Refund> PendingRefunds { get; set; } = new();

        // Statistics
        public EventStatisticsData EventStats { get; set; } = new();
        public List<VenueOccupancyData> VenueOccupancy { get; set; } = new();
        public RevenueStatisticsData RevenueStats { get; set; } = new();
        public PaymentStatisticsData PaymentStats { get; set; } = new();
    }

    public class EventStatisticsData
    {
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int CurrentEvents { get; set; }
        public int EndedEvents { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedEvents { get; set; }
        public int RejectedEvents { get; set; }
        public List<EventBase> UpcomingEventsList { get; set; } = new();
        public List<EventBase> CurrentEventsList { get; set; } = new();
        public List<EventBase> RecentlyEndedEvents { get; set; } = new();
    }

    public class VenueOccupancyData
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public bool IsCurrentlyOccupied { get; set; }
        public EventBase? CurrentEvent { get; set; }
        public int TotalEventsScheduled { get; set; }
    }

    public class RevenueStatisticsData
    {
        public decimal TotalRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalDiscountsGiven { get; set; }
        public decimal TotalRefunded { get; set; }
        public Dictionary<string, decimal> RevenueByEvent { get; set; } = new();
        public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();
        public Dictionary<string, int> BookingsByEvent { get; set; } = new();
    }

    public class PaymentStatisticsData
    {
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public int RefundedPayments { get; set; }
        public decimal TotalAmountProcessed { get; set; }
        public decimal TotalAmountRefunded { get; set; }
        public double SuccessRate { get; set; }
    }
}