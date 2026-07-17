using Core.Interfaces.ServicesInterface;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service for Organizer Dashboard Analytics
    /// </summary>
    public interface IOrganizerDashboardService
    {
        Task<OrganizerDashboardStatistics> GetDashboardStatisticsAsync(int organizerId, DateFilter filter);
        Task<byte[]> ExportFullReportAsync(int organizerId, DateFilter filter, string language = "en");
        Task<byte[]> ExportMonthlyReportAsync(int organizerId, int year, int month, string language = "en");
    }

    /// <summary>
    /// Complete Organizer Dashboard Statistics
    /// </summary>
    public class OrganizerDashboardStatistics
    {
        // Overview
        public OrganizerOverview Overview { get; set; } = new();

        // Financial
        public OrganizerFinancials Financials { get; set; } = new();

        // Events
        public OrganizerEvents Events { get; set; } = new();

        // Bookings & Tickets
        public OrganizerBookings Bookings { get; set; } = new();

        // Venues
        public OrganizerVenues Venues { get; set; } = new();

        // Time-series data for charts
        public List<TimeSeriesData> RevenueTimeSeries { get; set; } = new();
        public List<TimeSeriesData> TicketsTimeSeries { get; set; } = new();
        public List<TimeSeriesData> BookingsTimeSeries { get; set; } = new();

        // Event performance
        public List<EventPerformanceData> TopPerformingEvents { get; set; } = new();
    }

    public class OrganizerOverview
    {
        public int TotalEvents { get; set; }
        public int EventsThisPeriod { get; set; }
        public int ApprovedEvents { get; set; }
        public int PendingEvents { get; set; }
        public int RejectedEvents { get; set; }
        public int TotalVenues { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisPeriod { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TicketsSoldThisPeriod { get; set; }
    }

    public class OrganizerFinancials
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
        public int TotalRefundedTickets { get; set; }

        // Net Profit
        public decimal NetProfit { get; set; }
        public decimal NetProfitThisPeriod { get; set; }

        // Revenue by Currency
        public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();

        // Average revenue per event
        public decimal AverageRevenuePerEvent { get; set; }

        // Average ticket price
        public decimal AverageTicketPrice { get; set; }
    }

    public class OrganizerEvents
    {
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int CompletedEvents { get; set; }

        // By category
        public Dictionary<string, int> EventsByCategory { get; set; } = new();

        // By status
        public int ApprovedEvents { get; set; }
        public int PendingEvents { get; set; }
        public int RejectedEvents { get; set; }
    }

    public class OrganizerBookings
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

        // Conversion rate
        public double BookingConversionRate { get; set; }
    }

    public class OrganizerVenues
    {
        public int TotalVenues { get; set; }
        public int ApprovedVenues { get; set; }
        public int PendingVenues { get; set; }
        public int RejectedVenues { get; set; }
        public int CurrentlyOccupiedVenues { get; set; }

        // Most used venue
        public string? MostUsedVenueName { get; set; }
        public int MostUsedVenueEventCount { get; set; }
    }

    public class EventPerformanceData
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TicketsSold { get; set; }
        public int TotalBookings { get; set; }
        public double OccupancyRate { get; set; }
    }
}