using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Infrastructure;

using System;
using System.Collections.Generic;
using Core.Models.EventModels;
using Core.Models.PaymentModels;

namespace Entertania1.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        // Approval Queues
        public List<Core.Models.User> PendingOrganizers { get; set; } = new();
        public List<Core.Models.Venue> PendingVenues { get; set; } = new();
        public List<EventBase> PendingEvents { get; set; } = new();
        public List<Refund> PendingRefunds { get; set; } = new();

        // ✅ NEW: Event Statistics
        public EventStatistics EventStats { get; set; } = new();

        // ✅ NEW: Venue Statistics
        public List<VenueOccupancyInfo> VenueOccupancy { get; set; } = new();

        // ✅ NEW: Revenue Statistics
        public RevenueStatistics RevenueStats { get; set; } = new();

        // ✅ NEW: Payment Statistics
        public PaymentStatistics PaymentStats { get; set; } = new();
    }

    public class EventStatistics
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

    public class VenueOccupancyInfo
    {
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }
        public bool IsCurrentlyOccupied { get; set; }
        public EventBase? CurrentEvent { get; set; }
        public DateTime? NextAvailableDate { get; set; }
        public int TotalEventsScheduled { get; set; }
    }

    public class RevenueStatistics
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalRevenueEGP { get; set; }
        public decimal TotalTaxCollected { get; set; }
        public decimal TotalDiscountsGiven { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal NetRevenue { get; set; }

        public Dictionary<string, decimal> RevenueByEvent { get; set; } = new();
        public Dictionary<string, decimal> RevenueByCurrency { get; set; } = new();
        public Dictionary<string, int> BookingsByEvent { get; set; } = new();
    }

    public class PaymentStatistics
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