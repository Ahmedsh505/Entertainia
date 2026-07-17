using Core.Models;
using Core.Models.EventModels;
using Core.Models.PaymentModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Separate service for Admin Dashboard Analytics (Statistics Only)
    /// </summary>
    public interface IAdminDashboardService
    {
        Task<AdminDashboardStatistics> GetDashboardStatisticsAsync(DateFilter filter);
        Task<byte[]> ExportFullReportAsync(DateFilter filter, string language = "en");
    }

    /// <summary>
    /// Date filter for dashboard queries
    /// </summary>
    public class DateFilter
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public FilterPeriod Period { get; set; }

        public static DateFilter Last7Days => new DateFilter
        {
            Period = FilterPeriod.Last7Days,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow
        };

        public static DateFilter Last30Days => new DateFilter
        {
            Period = FilterPeriod.Last30Days,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow
        };

        public static DateFilter Last6Months => new DateFilter
        {
            Period = FilterPeriod.Last6Months,
            StartDate = DateTime.UtcNow.AddMonths(-6),
            EndDate = DateTime.UtcNow
        };

        public static DateFilter LastYear => new DateFilter
        {
            Period = FilterPeriod.LastYear,
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow
        };

        public static DateFilter Last5Years => new DateFilter
        {
            Period = FilterPeriod.Last5Years,
            StartDate = DateTime.UtcNow.AddYears(-5),
            EndDate = DateTime.UtcNow
        };

        public static DateFilter AllTime => new DateFilter
        {
            Period = FilterPeriod.AllTime,
            StartDate = DateTime.MinValue,
            EndDate = DateTime.UtcNow
        };
    }

    public enum FilterPeriod
    {
        Last7Days,
        Last30Days,
        Last6Months,
        LastYear,
        Last5Years,
        AllTime,
        Custom
    }

    /// <summary>
    /// Complete Admin Dashboard Statistics
    /// </summary>
    public class AdminDashboardStatistics
    {
        // Overview
        public OverviewStatistics Overview { get; set; } = new();

        // Financial
        public FinancialStatistics Financial { get; set; } = new();

        // Events
        public EventAnalytics Events { get; set; } = new();

        // Users
        public UserAnalytics Users { get; set; } = new();

        // Bookings & Tickets
        public BookingAnalytics Bookings { get; set; } = new();

        // Payments
        public PaymentAnalytics Payments { get; set; } = new();

        // Refunds
        public RefundAnalytics Refunds { get; set; } = new();

        // Time-series data for charts
        public List<TimeSeriesData> RevenueTimeSeries { get; set; } = new();
        public List<TimeSeriesData> TicketsTimeSeries { get; set; } = new();
        public List<TimeSeriesData> BookingsTimeSeries { get; set; } = new();

        // Top performers
        public List<TopEventData> TopEventsByRevenue { get; set; } = new();
        public List<TopOrganizerData> TopOrganizers { get; set; } = new();
    }

    public class OverviewStatistics
    {
        public int TotalUsers { get; set; }
        public int NewUsersThisPeriod { get; set; }
        public int TotalOrganizers { get; set; }
        public int NewOrganizersThisPeriod { get; set; }
        public int TotalEvents { get; set; }
        public int NewEventsThisPeriod { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisPeriod { get; set; }
    }

    public class FinancialStatistics
    {
        // Gross Revenue
        public decimal GrossRevenue { get; set; }
        public decimal GrossRevenueThisPeriod { get; set; }

        // Taxes
        public decimal TotalTaxCollected { get; set; }
        public decimal TaxCollectedThisPeriod { get; set; }

        // Discounts
        public decimal TotalDiscounts { get; set; }
        public decimal DiscountsThisPeriod { get; set; }

        // Refunds
        public decimal TotalRefunded { get; set; }
        public decimal RefundedThisPeriod { get; set; }

        // Net Profit (Gross - Tax - Discounts - Refunds)
        public decimal NetProfit { get; set; }
        public decimal NetProfitThisPeriod { get; set; }

        // Revenue by Currency
        public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();

        // Average transaction value
        public decimal AverageTransactionValue { get; set; }
    }

    public class EventAnalytics
    {
        public int TotalEvents { get; set; }
        public int ApprovedEvents { get; set; }
        public int PendingEvents { get; set; }
        public int RejectedEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int CompletedEvents { get; set; }

        // By category
        public Dictionary<string, int> EventsByCategory { get; set; } = new();

        // Average tickets per event
        public double AverageTicketsPerEvent { get; set; }
    }

    public class UserAnalytics
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisPeriod { get; set; }
        public int TotalOrganizers { get; set; }
        public int ActiveOrganizers { get; set; }
        public int PendingOrganizers { get; set; }
        public int RejectedOrganizers { get; set; }
        public int DeletedUsers { get; set; }
        public int BannedUsers { get; set; }
    }

    public class BookingAnalytics
    {
        public int TotalBookings { get; set; }
        public int BookingsThisPeriod { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int RefundedBookings { get; set; }

        // Tickets
        public int TotalTicketsSold { get; set; }
        public int TicketsSoldThisPeriod { get; set; }
        public int TicketsRefunded { get; set; }

        // By tier
        public Dictionary<string, int> TicketsByTier { get; set; } = new();
    }

    public class PaymentAnalytics
    {
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int PendingPayments { get; set; }
        public int FailedPayments { get; set; }
        public int RefundedPayments { get; set; }

        public decimal TotalAmountProcessed { get; set; }
        public decimal TotalAmountFailed { get; set; }

        // Success rate (excluding refunded from calculation)
        public double SuccessRate { get; set; }

        // Average processing time
        public double AverageProcessingTimeMinutes { get; set; }
    }

    public class RefundAnalytics
    {
        public int TotalRefundRequests { get; set; }
        public int RefundsThisPeriod { get; set; }
        public int PendingRefunds { get; set; }
        public int ApprovedRefunds { get; set; }
        public int RejectedRefunds { get; set; }

        public decimal TotalRefundAmount { get; set; }
        public decimal RefundAmountThisPeriod { get; set; }

        public double RefundRate { get; set; } // % of bookings refunded
        public double AverageRefundPercentage { get; set; }
    }

    public class TimeSeriesData
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int Count { get; set; }
    }

    public class TopEventData
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TicketsSold { get; set; }
        public int TotalBookings { get; set; }
    }

    public class TopOrganizerData
    {
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
    }
}