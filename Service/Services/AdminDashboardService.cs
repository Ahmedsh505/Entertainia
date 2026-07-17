using ClosedXML.Excel;
using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Service.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminDashboardService> _logger;
        private readonly IEgyptTimeService _timeService;
        public AdminDashboardService(IUnitOfWork unitOfWork, ILogger<AdminDashboardService> logger, IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _timeService = timeService;
        }

        public async Task<AdminDashboardStatistics> GetDashboardStatisticsAsync(DateFilter filter)
        {
            try
            {
                var stats = new AdminDashboardStatistics();

                // Get all data
                var allUsers = _unitOfWork.Users.GetAll();
                var allEvents = _unitOfWork.Events.GetAll();
                var allBookings = _unitOfWork.Bookings.GetAll().Where(b => b.Status == "Confirmed").ToList();
                var allPayments = _unitOfWork.Payments.GetAll();
                var allRefunds = _unitOfWork.Refunds.GetAll();

                // Filter by date
                var filteredBookings = allBookings.Where(b =>
                    b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate).ToList();
                var filteredPayments = allPayments.Where(p =>
                    p.CreatedAt >= filter.StartDate && p.CreatedAt <= filter.EndDate).ToList();

                // Build statistics
                stats.Overview = BuildOverviewStatistics(allUsers, allEvents, allBookings, filteredBookings, filter);
                stats.Financial = BuildFinancialStatistics(allBookings, filteredBookings, allRefunds);
                stats.Events = BuildEventAnalytics(allEvents, allBookings);
                stats.Users = BuildUserAnalytics(allUsers, filter);
                stats.Bookings = BuildBookingAnalytics(allBookings, filteredBookings);
                stats.Payments = BuildPaymentAnalytics(allPayments, filteredPayments);
                stats.Refunds = BuildRefundAnalytics(allRefunds, filter);

                // Time series
                stats.RevenueTimeSeries = BuildRevenueTimeSeries(allBookings, filter);
                stats.TicketsTimeSeries = BuildTicketsTimeSeries(allBookings, filter);
                stats.BookingsTimeSeries = BuildBookingsTimeSeries(allBookings, filter);

                // Top performers
                stats.TopEventsByRevenue = BuildTopEvents(allBookings);
                stats.TopOrganizers = BuildTopOrganizers(allBookings);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building admin dashboard statistics");
                throw;
            }
        }

        private OverviewStatistics BuildOverviewStatistics(
            List<Core.Models.User> users,
            List<Core.Models.EventModels.EventBase> events,
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings,
            DateFilter filter)
        {
            return new OverviewStatistics
            {
                TotalUsers = users.Count(u => !u.IsDeleted),
                NewUsersThisPeriod = users.Count(u => !u.IsDeleted && u.CreatedAt >= filter.StartDate),
                TotalOrganizers = users.Count(u => !u.IsDeleted && u.Role == "Organizer"),
                NewOrganizersThisPeriod = users.Count(u => !u.IsDeleted && u.Role == "Organizer" && u.CreatedAt >= filter.StartDate),
                TotalEvents = events.Count,
                NewEventsThisPeriod = events.Count(e => e.CreatedAt >= filter.StartDate && e.CreatedAt <= filter.EndDate),
                TotalRevenue = allBookings.Sum(b => b.TotalWithTax),
                RevenueThisPeriod = periodBookings.Sum(b => b.TotalWithTax)
            };
        }

        private FinancialStatistics BuildFinancialStatistics(
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings,
            List<Core.Models.PaymentModels.Refund> allRefunds)
        {
            var completedRefunds = allRefunds.Where(r => r.Status == PaymentStatus.Completed).ToList();

            var grossAll = allBookings.Sum(b => b.TotalPrice);
            var grossPeriod = periodBookings.Sum(b => b.TotalPrice);
            var taxAll = allBookings.Sum(b => b.TaxAmount);
            var taxPeriod = periodBookings.Sum(b => b.TaxAmount);
            var discountsAll = allBookings.Sum(b => b.DiscountAmount);
            var discountsPeriod = periodBookings.Sum(b => b.DiscountAmount);
            var refundsAll = completedRefunds.Sum(r => r.RefundAmount);
            var refundsPeriod = completedRefunds.Where(r => r.CompletedAt.HasValue && r.CompletedAt >= periodBookings.Min(b => b.BookingDate)).Sum(r => r.RefundAmount);

            return new FinancialStatistics
            {
                GrossRevenue = grossAll,
                GrossRevenueThisPeriod = grossPeriod,
                TotalTaxCollected = taxAll,
                TaxCollectedThisPeriod = taxPeriod,
                TotalDiscounts = discountsAll,
                DiscountsThisPeriod = discountsPeriod,
                TotalRefunded = refundsAll,
                RefundedThisPeriod = refundsPeriod,
                NetProfit = grossAll - refundsAll,
                NetProfitThisPeriod = grossPeriod - refundsPeriod,
                RevenueByCurrency = allBookings
                    .GroupBy(b => b.Currency.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalWithTax)),
                AverageTransactionValue = allBookings.Any() ? allBookings.Average(b => b.TotalWithTax) : 0
            };
        }

        private EventAnalytics BuildEventAnalytics(
            List<Core.Models.EventModels.EventBase> events,
            List<Core.Models.BookingModels.Booking> bookings)
        {
            var now = DateTime.UtcNow;

            return new EventAnalytics
            {
                TotalEvents = events.Count,
                ApprovedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Approved),
                PendingEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Pending),
                RejectedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Rejected),
                UpcomingEvents = events.Count(e => e.StartDate > now && e.ApprovalStatus == ApprovalStatus.Approved),
                OngoingEvents = events.Count(e => e.StartDate <= now && e.EndDate >= now && e.ApprovalStatus == ApprovalStatus.Approved),
                CompletedEvents = events.Count(e => e.EndDate < now),
                EventsByCategory = events.GroupBy(e => e.Category ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                AverageTicketsPerEvent = events.Any() ? bookings.GroupBy(b => b.EventId).Average(g => g.Sum(b => b.NumberOfTickets)) : 0
            };
        }

        private UserAnalytics BuildUserAnalytics(List<Core.Models.User> users, DateFilter filter)
        {
            return new UserAnalytics
            {
                TotalCustomers = users.Count(u => !u.IsDeleted && u.Role == "Customer"),
                NewCustomersThisPeriod = users.Count(u => !u.IsDeleted && u.Role == "Customer" && u.CreatedAt >= filter.StartDate),
                TotalOrganizers = users.Count(u => !u.IsDeleted && u.Role == "Organizer"),
                ActiveOrganizers = users.Count(u => !u.IsDeleted && u.Role == "Organizer" && u.AccountStatus == AccountStatus.Active),
                PendingOrganizers = users.Count(u => !u.IsDeleted && u.Role == "Organizer" && u.AccountStatus == AccountStatus.PendingApproval),
                RejectedOrganizers = users.Count(u => !u.IsDeleted && u.Role == "Organizer" && u.AccountStatus == AccountStatus.Rejected),
                DeletedUsers = users.Count(u => u.IsDeleted && u.DeletionType != DeletionType.AdminBanned),
                BannedUsers = users.Count(u => u.IsDeleted && u.DeletionType == DeletionType.AdminBanned)
            };
        }

        private BookingAnalytics BuildBookingAnalytics(
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings)
        {
            return new BookingAnalytics
            {
                TotalBookings = allBookings.Count,
                BookingsThisPeriod = periodBookings.Count,
                ConfirmedBookings = allBookings.Count(b => b.Status == "Confirmed"),
                PendingBookings = allBookings.Count(b => b.Status == "Pending" || b.Status == "PendingPayment"),
                CancelledBookings = allBookings.Count(b => b.Status == "Cancelled"),
                RefundedBookings = allBookings.Count(b => b.Status == "Refunded"),
                TotalTicketsSold = allBookings.Where(b => b.Status == "Confirmed").Sum(b => b.NumberOfTickets),
                TicketsSoldThisPeriod = periodBookings.Sum(b => b.NumberOfTickets),
                TicketsRefunded = allBookings.Where(b => b.Status == "Refunded").Sum(b => b.NumberOfTickets),
                TicketsByTier = allBookings.Where(b => b.Status == "Confirmed")
                    .GroupBy(b => b.TicketTier.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.NumberOfTickets))
            };
        }

        private PaymentAnalytics BuildPaymentAnalytics(
            List<Core.Models.PaymentModels.Payment> allPayments,
            List<Core.Models.PaymentModels.Payment> periodPayments)
        {
            var completedPayments = allPayments.Where(p => p.Status == PaymentStatus.Completed).ToList();
            var totalAttempts = allPayments.Count(p => p.Status != PaymentStatus.Pending);

            return new PaymentAnalytics
            {
                TotalPayments = allPayments.Count,
                CompletedPayments = completedPayments.Count,
                PendingPayments = allPayments.Count(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing),
                FailedPayments = allPayments.Count(p => p.Status == PaymentStatus.Failed),
                RefundedPayments = allPayments.Count(p => p.Status == PaymentStatus.Refunded || p.Status == PaymentStatus.PartiallyRefunded),
                TotalAmountProcessed = completedPayments.Sum(p => p.TotalAmountWithTax),
                TotalAmountFailed = allPayments.Where(p => p.Status == PaymentStatus.Failed).Sum(p => p.TotalAmountWithTax),
                SuccessRate = totalAttempts > 0 ? (double)completedPayments.Count / totalAttempts * 100 : 0,
                AverageProcessingTimeMinutes = completedPayments.Where(p => p.CompletedAt.HasValue)
                    .Select(p => (p.CompletedAt!.Value - p.CreatedAt).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Average()
            };
        }

        private RefundAnalytics BuildRefundAnalytics(List<Core.Models.PaymentModels.Refund> refunds, DateFilter filter)
        {
            var periodRefunds = refunds.Where(r => r.RequestedAt >= filter.StartDate && r.RequestedAt <= filter.EndDate).ToList();
            var approvedRefunds = refunds.Where(r => r.Status == PaymentStatus.Completed).ToList();

            var totalBookings = _unitOfWork.Bookings.GetAll().Count(b => b.Status == "Confirmed");
            var refundedBookings = refunds.Count(r => r.Status == PaymentStatus.Completed);

            return new RefundAnalytics
            {
                TotalRefundRequests = refunds.Count,
                RefundsThisPeriod = periodRefunds.Count,
                PendingRefunds = refunds.Count(r => r.Status == PaymentStatus.Pending),
                ApprovedRefunds = approvedRefunds.Count,
                RejectedRefunds = refunds.Count(r => r.Status == PaymentStatus.Failed),
                TotalRefundAmount = approvedRefunds.Sum(r => r.RefundAmount),
                RefundAmountThisPeriod = periodRefunds.Where(r => r.Status == PaymentStatus.Completed).Sum(r => r.RefundAmount),
                RefundRate = totalBookings > 0 ? (double)refundedBookings / totalBookings * 100 : 0,
                AverageRefundPercentage = approvedRefunds.Any() ? (double)approvedRefunds.Average(r => r.RefundPercentage) : 80
            };
        }

        private List<TimeSeriesData> BuildRevenueTimeSeries(List<Core.Models.BookingModels.Booking> bookings, DateFilter filter)
        {
            var groupedData = bookings
                .Where(b => b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate)
                .GroupBy(b => GetTimeSeriesKey(b.BookingDate, filter.Period))
                .Select(g => new TimeSeriesData
                {
                    Date = g.Key,
                    Label = FormatTimeSeriesLabel(g.Key, filter.Period),
                    Value = g.Sum(b => b.TotalWithTax),
                    Count = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return groupedData;
        }

        private List<TimeSeriesData> BuildTicketsTimeSeries(List<Core.Models.BookingModels.Booking> bookings, DateFilter filter)
        {
            var groupedData = bookings
                .Where(b => b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate && b.Status == "Confirmed")
                .GroupBy(b => GetTimeSeriesKey(b.BookingDate, filter.Period))
                .Select(g => new TimeSeriesData
                {
                    Date = g.Key,
                    Label = FormatTimeSeriesLabel(g.Key, filter.Period),
                    Value = g.Sum(b => b.NumberOfTickets),
                    Count = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            return groupedData;
        }

        private List<TimeSeriesData> BuildBookingsTimeSeries(List<Core.Models.BookingModels.Booking> bookings, DateFilter filter)
        {
            return bookings
                .Where(b => b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate)
                .GroupBy(b => GetTimeSeriesKey(b.BookingDate, filter.Period))
                .Select(g => new TimeSeriesData
                {
                    Date = g.Key,
                    Label = FormatTimeSeriesLabel(g.Key, filter.Period),
                    Value = g.Count(),
                    Count = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList();
        }

        private List<TopEventData> BuildTopEvents(List<Core.Models.BookingModels.Booking> bookings)
        {
            return bookings
                .Where(b => b.Status == "Confirmed")
                .GroupBy(b => new { b.EventId, b.Event!.Title, b.Event.Category })
                .Select(g => new TopEventData
                {
                    EventId = g.Key.EventId,
                    EventTitle = g.Key.Title ?? "Unknown",
                    Category = g.Key.Category ?? "Unknown",
                    TotalRevenue = g.Sum(b => b.TotalWithTax),
                    TicketsSold = g.Sum(b => b.NumberOfTickets),
                    TotalBookings = g.Count()
                })
                .OrderByDescending(e => e.TotalRevenue)
                .Take(10)
                .ToList();
        }

        private List<TopOrganizerData> BuildTopOrganizers(List<Core.Models.BookingModels.Booking> bookings)
        {
            return bookings
                .Where(b => b.Status == "Confirmed" && b.Event != null && b.Event.Organizer != null)
                .GroupBy(b => new
                {
                    b.Event!.OrganizerId,
                    OrganizerName = $"{b.Event.Organizer!.FirstName} {b.Event.Organizer.LastName}"
                })
                .Select(g => new TopOrganizerData
                {
                    OrganizerId = g.Key.OrganizerId,
                    OrganizerName = g.Key.OrganizerName,
                    TotalEvents = g.Select(b => b.EventId).Distinct().Count(),
                    TotalRevenue = g.Sum(b => b.TotalWithTax),
                    TotalTicketsSold = g.Sum(b => b.NumberOfTickets)
                })
                .OrderByDescending(o => o.TotalRevenue)
                .Take(10)
                .ToList();
        }

        private DateTime GetTimeSeriesKey(DateTime date, FilterPeriod period)
        {
            return period switch
            {
                FilterPeriod.Last7Days => date.Date,
                FilterPeriod.Last30Days => date.Date,
                FilterPeriod.Last6Months => new DateTime(date.Year, date.Month, 1),
                FilterPeriod.LastYear => new DateTime(date.Year, date.Month, 1),
                FilterPeriod.Last5Years => new DateTime(date.Year, date.Month, 1),
                _ => date.Date
            };
        }

        private string FormatTimeSeriesLabel(DateTime date, FilterPeriod period)
        {
            return period switch
            {
                FilterPeriod.Last7Days => date.ToString("ddd, MMM dd"),
                FilterPeriod.Last30Days => date.ToString("MMM dd"),
                FilterPeriod.Last6Months => date.ToString("MMM yyyy"),
                FilterPeriod.LastYear => date.ToString("MMM yyyy"),
                FilterPeriod.Last5Years => date.ToString("MMM yyyy"),
                _ => date.ToString("yyyy-MM-dd")
            };
        }

        public async Task<byte[]> ExportFullReportAsync(DateFilter filter, string language = "en")
        {
            var stats = await GetDashboardStatisticsAsync(filter);
            var isArabic = language == "ar";

            using var workbook = new XLWorkbook();

            // Note: ClosedXML doesn't have workbook-level RTL property
            // RTL is set at worksheet level in AddSectionHeader method

            // Sheet 1: Overview
            AddOverviewSheet(workbook, stats, filter, isArabic);

            // Sheet 2: Financial Details
            AddFinancialSheet(workbook, stats, isArabic);

            // Sheet 3: Events Analysis
            AddEventsSheet(workbook, stats, isArabic);

            // Sheet 4: Users & Organizers
            AddUsersSheet(workbook, stats, isArabic);

            // Sheet 5: Bookings & Tickets
            AddBookingsSheet(workbook, stats, isArabic);

            // Sheet 6: Payments Analysis
            AddPaymentsSheet(workbook, stats, isArabic);

            // Sheet 7: Refunds Analysis
            AddRefundsSheet(workbook, stats, isArabic);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private void AddOverviewSheet(XLWorkbook workbook, AdminDashboardStatistics stats, DateFilter filter, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "نظرة عامة" : "Overview");

            // Headers
            ws.Cell("A1").Value = isArabic ? "تقرير لوحة التحكم الإدارية" : "Admin Dashboard Report";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Range("A1:D1").Merge();

            ws.Cell("A2").Value = isArabic ? $"الفترة: {filter.StartDate:yyyy-MM-dd} إلى {filter.EndDate:yyyy-MM-dd}"
                : $"Period: {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Range("A2:D2").Merge();

            int row = 4;

            // Users Section
            AddSectionHeader(ws, row++, isArabic ? "إحصائيات المستخدمين" : "User Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المستخدمين" : "Total Users", stats.Overview.TotalUsers, isArabic);
            AddDataRow(ws, row++, isArabic ? "مستخدمون جدد (هذه الفترة)" : "New Users (This Period)", stats.Overview.NewUsersThisPeriod, isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المنظمين" : "Total Organizers", stats.Overview.TotalOrganizers, isArabic);
            AddDataRow(ws, row++, isArabic ? "منظمون جدد (هذه الفترة)" : "New Organizers (This Period)", stats.Overview.NewOrganizersThisPeriod, isArabic);

            row++;

            // Events Section
            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الفعاليات" : "Event Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الفعاليات" : "Total Events", stats.Overview.TotalEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "فعاليات جديدة (هذه الفترة)" : "New Events (This Period)", stats.Overview.NewEventsThisPeriod, isArabic);

            row++;

            // Revenue Section
            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الإيرادات" : "Revenue Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الإيرادات" : "Total Revenue", stats.Overview.TotalRevenue.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "إيرادات هذه الفترة" : "Revenue This Period", stats.Overview.RevenueThisPeriod.ToString("N2"), isArabic);

            ws.Columns().AdjustToContents();
        }

        private void AddFinancialSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "التفاصيل المالية" : "Financial Details");

            ws.Cell("A1").Value = isArabic ? "التقرير المالي التفصيلي" : "Detailed Financial Report";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Range("A1:D1").Merge();

            int row = 3;

            // All Time
            AddSectionHeader(ws, row++, isArabic ? "إجمالي الوقت" : "All Time", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الإيرادات" : "Gross Revenue", stats.Financial.GrossRevenue.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الضرائب المحصلة" : "Tax Collected", stats.Financial.TotalTaxCollected.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الخصومات المقدمة" : "Discounts Given", stats.Financial.TotalDiscounts.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "المبالغ المستردة" : "Amount Refunded", stats.Financial.TotalRefunded.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "صافي الربح" : "Net Profit", stats.Financial.NetProfit.ToString("N2"), isArabic, true);

            row++;

            // This Period
            AddSectionHeader(ws, row++, isArabic ? "هذه الفترة" : "This Period", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الإيرادات" : "Gross Revenue", stats.Financial.GrossRevenueThisPeriod.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الضرائب المحصلة" : "Tax Collected", stats.Financial.TaxCollectedThisPeriod.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الخصومات المقدمة" : "Discounts Given", stats.Financial.DiscountsThisPeriod.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "المبالغ المستردة" : "Amount Refunded", stats.Financial.RefundedThisPeriod.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "صافي الربح" : "Net Profit", stats.Financial.NetProfitThisPeriod.ToString("N2"), isArabic, true);

            row += 2;

            // Revenue by Currency
            AddSectionHeader(ws, row++, isArabic ? "الإيرادات حسب العملة" : "Revenue by Currency", isArabic);
            foreach (var curr in stats.Financial.RevenueByCurrency)
            {
                AddDataRow(ws, row++, curr.Key, curr.Value.ToString("N2"), isArabic);
            }

            ws.Columns().AdjustToContents();
        }

        private void AddEventsSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "تحليل الفعاليات" : "Events Analysis");

            ws.Cell("A1").Value = isArabic ? "تحليل الفعاليات" : "Events Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الفعاليات" : "Event Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الفعاليات" : "Total Events", stats.Events.TotalEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات الموافق عليها" : "Approved Events", stats.Events.ApprovedEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات المعلقة" : "Pending Events", stats.Events.PendingEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات المرفوضة" : "Rejected Events", stats.Events.RejectedEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات القادمة" : "Upcoming Events", stats.Events.UpcomingEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات الجارية" : "Ongoing Events", stats.Events.OngoingEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات المكتملة" : "Completed Events", stats.Events.CompletedEvents, isArabic);

            row += 2;

            // Events by Category
            AddSectionHeader(ws, row++, isArabic ? "الفعاليات حسب الفئة" : "Events by Category", isArabic);
            foreach (var cat in stats.Events.EventsByCategory)
            {
                AddDataRow(ws, row++, cat.Key, cat.Value.ToString(), isArabic);
            }

            row += 2;

            // Top Events
            AddSectionHeader(ws, row++, isArabic ? "أفضل الفعاليات" : "Top Events by Revenue", isArabic);
            row++;

            // Table headers
            ws.Cell(row, 1).Value = isArabic ? "عنوان الفعالية" : "Event Title";
            ws.Cell(row, 2).Value = isArabic ? "الفئة" : "Category";
            ws.Cell(row, 3).Value = isArabic ? "الإيرادات" : "Revenue";
            ws.Cell(row, 4).Value = isArabic ? "التذاكر المباعة" : "Tickets Sold";
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var evt in stats.TopEventsByRevenue)
            {
                ws.Cell(row, 1).Value = evt.EventTitle;
                ws.Cell(row, 2).Value = evt.Category;
                ws.Cell(row, 3).Value = evt.TotalRevenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = evt.TicketsSold;
                row++;
            }

            ws.Columns().AdjustToContents();
        }

        private void AddUsersSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "المستخدمون والمنظمون" : "Users & Organizers");

            ws.Cell("A1").Value = isArabic ? "تحليل المستخدمين" : "Users Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات العملاء" : "Customer Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي العملاء" : "Total Customers", stats.Users.TotalCustomers, isArabic);
            AddDataRow(ws, row++, isArabic ? "عملاء جدد (هذه الفترة)" : "New Customers (This Period)", stats.Users.NewCustomersThisPeriod, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات المنظمين" : "Organizer Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المنظمين" : "Total Organizers", stats.Users.TotalOrganizers, isArabic);
            AddDataRow(ws, row++, isArabic ? "المنظمون النشطون" : "Active Organizers", stats.Users.ActiveOrganizers, isArabic);
            AddDataRow(ws, row++, isArabic ? "المنظمون المعلقون" : "Pending Organizers", stats.Users.PendingOrganizers, isArabic);
            AddDataRow(ws, row++, isArabic ? "المنظمون المرفوضون" : "Rejected Organizers", stats.Users.RejectedOrganizers, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "حسابات محذوفة/محظورة" : "Deleted/Banned Accounts", isArabic);
            AddDataRow(ws, row++, isArabic ? "المستخدمون المحذوفون" : "Deleted Users", stats.Users.DeletedUsers, isArabic);
            AddDataRow(ws, row++, isArabic ? "المستخدمون المحظورون" : "Banned Users", stats.Users.BannedUsers, isArabic);

            row += 2;

            // Top Organizers
            AddSectionHeader(ws, row++, isArabic ? "أفضل المنظمين" : "Top Organizers", isArabic);
            row++;

            ws.Cell(row, 1).Value = isArabic ? "اسم المنظم" : "Organizer Name";
            ws.Cell(row, 2).Value = isArabic ? "عدد الفعاليات" : "Events Count";
            ws.Cell(row, 3).Value = isArabic ? "الإيرادات" : "Revenue";
            ws.Cell(row, 4).Value = isArabic ? "التذاكر المباعة" : "Tickets Sold";
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var org in stats.TopOrganizers)
            {
                ws.Cell(row, 1).Value = org.OrganizerName;
                ws.Cell(row, 2).Value = org.TotalEvents;
                ws.Cell(row, 3).Value = org.TotalRevenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = org.TotalTicketsSold;
                row++;
            }

            ws.Columns().AdjustToContents();
        }

        private void AddBookingsSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "الحجوزات والتذاكر" : "Bookings & Tickets");

            ws.Cell("A1").Value = isArabic ? "تحليل الحجوزات والتذاكر" : "Bookings & Tickets Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الحجوزات" : "Booking Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الحجوزات" : "Total Bookings", stats.Bookings.TotalBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "حجوزات هذه الفترة" : "Bookings This Period", stats.Bookings.BookingsThisPeriod, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات المؤكدة" : "Confirmed Bookings", stats.Bookings.ConfirmedBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات المعلقة" : "Pending Bookings", stats.Bookings.PendingBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات الملغاة" : "Cancelled Bookings", stats.Bookings.CancelledBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات المستردة" : "Refunded Bookings", stats.Bookings.RefundedBookings, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات التذاكر" : "Ticket Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي التذاكر المباعة" : "Total Tickets Sold", stats.Bookings.TotalTicketsSold, isArabic);
            AddDataRow(ws, row++, isArabic ? "تذاكر مباعة (هذه الفترة)" : "Tickets Sold (This Period)", stats.Bookings.TicketsSoldThisPeriod, isArabic);
            AddDataRow(ws, row++, isArabic ? "التذاكر المستردة" : "Tickets Refunded", stats.Bookings.TicketsRefunded, isArabic);

            row += 2;

            // Tickets by Tier
            AddSectionHeader(ws, row++, isArabic ? "التذاكر حسب الفئة" : "Tickets by Tier", isArabic);
            foreach (var tier in stats.Bookings.TicketsByTier)
            {
                AddDataRow(ws, row++, tier.Key, tier.Value.ToString(), isArabic);
            }

            ws.Columns().AdjustToContents();
        }

        private void AddPaymentsSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "تحليل المدفوعات" : "Payments Analysis");

            ws.Cell("A1").Value = isArabic ? "تحليل المدفوعات" : "Payments Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات المدفوعات" : "Payment Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المدفوعات" : "Total Payments", stats.Payments.TotalPayments, isArabic);
            AddDataRow(ws, row++, isArabic ? "المدفوعات المكتملة" : "Completed Payments", stats.Payments.CompletedPayments, isArabic);
            AddDataRow(ws, row++, isArabic ? "المدفوعات المعلقة" : "Pending Payments", stats.Payments.PendingPayments, isArabic);
            AddDataRow(ws, row++, isArabic ? "المدفوعات الفاشلة" : "Failed Payments", stats.Payments.FailedPayments, isArabic);
            AddDataRow(ws, row++, isArabic ? "المدفوعات المستردة" : "Refunded Payments", stats.Payments.RefundedPayments, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "المبالغ المالية" : "Financial Amounts", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المبلغ المعالج" : "Total Amount Processed", stats.Payments.TotalAmountProcessed.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المبلغ الفاشل" : "Total Amount Failed", stats.Payments.TotalAmountFailed.ToString("N2"), isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "معدلات الأداء" : "Performance Metrics", isArabic);
            AddDataRow(ws, row++, isArabic ? "معدل النجاح" : "Success Rate", $"{stats.Payments.SuccessRate:F2}%", isArabic);
            AddDataRow(ws, row++, isArabic ? "متوسط وقت المعالجة (دقائق)" : "Avg Processing Time (min)", stats.Payments.AverageProcessingTimeMinutes.ToString("F2"), isArabic);

            ws.Columns().AdjustToContents();
        }

        private void AddRefundsSheet(XLWorkbook workbook, AdminDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "تحليل الاستردادات" : "Refunds Analysis");

            ws.Cell("A1").Value = isArabic ? "تحليل الاستردادات" : "Refunds Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الاستردادات" : "Refund Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي طلبات الاسترداد" : "Total Refund Requests", stats.Refunds.TotalRefundRequests, isArabic);
            AddDataRow(ws, row++, isArabic ? "استردادات هذه الفترة" : "Refunds This Period", stats.Refunds.RefundsThisPeriod, isArabic);
            AddDataRow(ws, row++, isArabic ? "الاستردادات المعلقة" : "Pending Refunds", stats.Refunds.PendingRefunds, isArabic);
            AddDataRow(ws, row++, isArabic ? "الاستردادات الموافق عليها" : "Approved Refunds", stats.Refunds.ApprovedRefunds, isArabic);
            AddDataRow(ws, row++, isArabic ? "الاستردادات المرفوضة" : "Rejected Refunds", stats.Refunds.RejectedRefunds, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "المبالغ المالية" : "Financial Amounts", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي المبلغ المسترد" : "Total Refund Amount", stats.Refunds.TotalRefundAmount.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "مبلغ مسترد (هذه الفترة)" : "Refund Amount (This Period)", stats.Refunds.RefundAmountThisPeriod.ToString("N2"), isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "معدلات الاسترداد" : "Refund Rates", isArabic);
            AddDataRow(ws, row++, isArabic ? "معدل الاسترداد" : "Refund Rate", $"{stats.Refunds.RefundRate:F2}%", isArabic);
            AddDataRow(ws, row++, isArabic ? "متوسط نسبة الاسترداد" : "Avg Refund Percentage", $"{stats.Refunds.AverageRefundPercentage:F2}%", isArabic);

            ws.Columns().AdjustToContents();
        }

        private void AddSectionHeader(IXLWorksheet ws, int row, string title, bool isArabic)
        {
            ws.Cell(row, 1).Value = title;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            ws.Range(row, 1, row, 4).Merge();

            if (isArabic)
            {
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                // Set worksheet RTL for Arabic
                ws.RightToLeft = true;
            }
        }

        private void AddDataRow(IXLWorksheet ws, int row, string label, object value, bool isArabic, bool highlight = false)
        {
            ws.Cell(row, 1).Value = label;

            // Handle type conversion for ClosedXML
            if (value is string strValue)
            {
                ws.Cell(row, 2).Value = strValue;
            }
            else if (value is int intValue)
            {
                ws.Cell(row, 2).Value = intValue;
            }
            else if (value is decimal decValue)
            {
                ws.Cell(row, 2).Value = decValue;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            }
            else if (value is double dblValue)
            {
                ws.Cell(row, 2).Value = dblValue;
            }
            else
            {
                ws.Cell(row, 2).Value = value?.ToString() ?? "";
            }

            if (highlight)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
            }

            if (isArabic)
            {
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
        }
    }
}