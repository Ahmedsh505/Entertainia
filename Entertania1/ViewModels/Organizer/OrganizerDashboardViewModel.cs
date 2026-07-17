using Core.Models.EventModels;
using System.Collections.Generic;

namespace Entertania1.ViewModels.Organizer
{
    public class OrganizerDashboardViewModel
    {
        // ===== BASIC STATS =====
        public int TotalEvents { get; set; }
        public int ApprovedEvents { get; set; }
        public int PendingEvents { get; set; }
        public int VenuesCount { get; set; }

        // ===== REVENUE & TICKETS =====
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public int MonthlyTicketsSold { get; set; }

        // ===== FINANCIAL STATS =====
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalDiscountsGiven { get; set; }
        public decimal NetRevenue => TotalRevenue - TotalTaxCollected - TotalDiscountsGiven;

        // ===== RECENT ITEMS =====
        public List<EventBase> RecentEvents { get; set; } = new();
        public List<Core.Models.Venue> MyVenues { get; set; } = new();

        // ===== CHART DATA =====
        // Labels for months (e.g., ["Jan", "Feb", "Mar"])
        public List<string> RevenueMonths { get; set; } = new();

        // Data for each chart
        public List<decimal> MonthlyRevenueData { get; set; } = new();   // Revenue per month
        public List<int> MonthlyTicketsData { get; set; } = new();       // Tickets per month
    }
}