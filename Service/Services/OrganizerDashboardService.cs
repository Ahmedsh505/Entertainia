using ClosedXML.Excel;
using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    public class OrganizerDashboardService : IOrganizerDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrganizerDashboardService> _logger;
        private readonly IEgyptTimeService _timeService;
        public OrganizerDashboardService(IUnitOfWork unitOfWork, ILogger<OrganizerDashboardService> logger, IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _timeService = timeService;
        }

        public async Task<OrganizerDashboardStatistics> GetDashboardStatisticsAsync(int organizerId, DateFilter filter)
        {
            try
            {
                var stats = new OrganizerDashboardStatistics();

                // Get organizer's data
                var allEvents = _unitOfWork.Events.GetByOrganizer(organizerId);
                var allBookings = _unitOfWork.Bookings.GetAll()
                    .Where(b => b.Event.OrganizerId == organizerId && b.Status == "Confirmed")
                    .ToList();
                var allVenues = _unitOfWork.Venues.GetByUser(organizerId);
                var allRefunds = _unitOfWork.Refunds.GetAll()
                    .Where(r => r.Booking != null && r.Booking.Event.OrganizerId == organizerId)
                    .ToList();

                // Filter by date
                var periodBookings = allBookings
                    .Where(b => b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate)
                    .ToList();

                // Build statistics
                stats.Overview = BuildOverview(allEvents, allBookings, periodBookings, allVenues, filter);
                stats.Financials = BuildFinancials(allBookings, periodBookings, allRefunds, filter);
                stats.Events = BuildEvents(allEvents);
                stats.Bookings = BuildBookings(allBookings, periodBookings);
                stats.Venues = BuildVenues(allVenues, allEvents);

                // Time series
                stats.RevenueTimeSeries = BuildRevenueTimeSeries(allBookings, filter);
                stats.TicketsTimeSeries = BuildTicketsTimeSeries(allBookings, filter);
                stats.BookingsTimeSeries = BuildBookingsTimeSeries(allBookings, filter);

                // Event performance
                stats.TopPerformingEvents = BuildEventPerformance(allEvents, allBookings);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error building dashboard statistics for organizer {organizerId}");
                throw;
            }
        }

        private OrganizerOverview BuildOverview(
            List<Core.Models.EventModels.EventBase> events,
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings,
            List<Core.Models.Venue> venues,
            DateFilter filter)
        {
            return new OrganizerOverview
            {
                TotalEvents = events.Count,
                EventsThisPeriod = events.Count(e => e.CreatedAt >= filter.StartDate && e.CreatedAt <= filter.EndDate),
                ApprovedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Approved),
                PendingEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Pending),
                RejectedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Rejected),
                TotalVenues = venues.Count,
                TotalRevenue = allBookings.Sum(b => b.TotalWithTax),
                RevenueThisPeriod = periodBookings.Sum(b => b.TotalWithTax),
                TotalTicketsSold = allBookings.Sum(b => b.NumberOfTickets),
                TicketsSoldThisPeriod = periodBookings.Sum(b => b.NumberOfTickets)
            };
        }

        private OrganizerFinancials BuildFinancials(
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings,
            List<Core.Models.PaymentModels.Refund> allRefunds,
            DateFilter filter)
        {
            var completedRefunds = allRefunds.Where(r => r.Status == PaymentStatus.Completed).ToList();
            var periodRefunds = completedRefunds
                .Where(r => r.CompletedAt >= filter.StartDate && r.CompletedAt <= filter.EndDate)
                .ToList();

            var grossAll = allBookings.Sum(b => b.TotalPrice);
            var grossPeriod = periodBookings.Sum(b => b.TotalPrice);
            var taxAll = allBookings.Sum(b => b.TaxAmount);
            var taxPeriod = periodBookings.Sum(b => b.TaxAmount);
            var discountsAll = allBookings.Sum(b => b.DiscountAmount);
            var discountsPeriod = periodBookings.Sum(b => b.DiscountAmount);
            var refundsAll = completedRefunds.Sum(r => r.RefundAmount);
            var refundsPeriod = periodRefunds.Sum(r => r.RefundAmount);

            return new OrganizerFinancials
            {
                GrossRevenue = grossAll,
                GrossRevenueThisPeriod = grossPeriod,
                TotalTaxCollected = taxAll,
                TaxCollectedThisPeriod = taxPeriod,
                TotalDiscounts = discountsAll,
                DiscountsThisPeriod = discountsPeriod,
                TotalRefunded = refundsAll,
                RefundedThisPeriod = refundsPeriod,
                TotalRefundedTickets = completedRefunds.Sum(r => r.Booking?.NumberOfTickets ?? 0),
                NetProfit = grossAll - refundsAll,
                NetProfitThisPeriod = grossPeriod - refundsPeriod,
                RevenueByCurrency = allBookings
                    .GroupBy(b => b.Currency.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalWithTax)),
                AverageRevenuePerEvent = allBookings.Any() ?
                    allBookings.GroupBy(b => b.EventId).Average(g => g.Sum(b => b.TotalWithTax)) : 0,
                AverageTicketPrice = allBookings.Any() ?
                    allBookings.Sum(b => b.TotalPrice) / allBookings.Sum(b => b.NumberOfTickets) : 0
            };
        }

        private OrganizerEvents BuildEvents(List<Core.Models.EventModels.EventBase> events)
        {
            var now = DateTime.UtcNow;

            return new OrganizerEvents
            {
                TotalEvents = events.Count,
                UpcomingEvents = events.Count(e => e.StartDate > now && e.ApprovalStatus == ApprovalStatus.Approved),
                OngoingEvents = events.Count(e => e.StartDate <= now && e.EndDate >= now && e.ApprovalStatus == ApprovalStatus.Approved),
                CompletedEvents = events.Count(e => e.EndDate < now),
                EventsByCategory = events.GroupBy(e => e.Category ?? "Unknown").ToDictionary(g => g.Key, g => g.Count()),
                ApprovedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Approved),
                PendingEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Pending),
                RejectedEvents = events.Count(e => e.ApprovalStatus == ApprovalStatus.Rejected)
            };
        }

        private OrganizerBookings BuildBookings(
            List<Core.Models.BookingModels.Booking> allBookings,
            List<Core.Models.BookingModels.Booking> periodBookings)
        {
            var allOrgBookings = _unitOfWork.Bookings.GetAll()
                .Where(b => allBookings.Select(ab => ab.EventId).Contains(b.EventId))
                .ToList();

            return new OrganizerBookings
            {
                TotalBookings = allBookings.Count,
                BookingsThisPeriod = periodBookings.Count,
                ConfirmedBookings = allBookings.Count(b => b.Status == "Confirmed"),
                PendingBookings = allOrgBookings.Count(b => b.Status == "Pending" || b.Status == "PendingPayment"),
                CancelledBookings = allOrgBookings.Count(b => b.Status == "Cancelled"),
                RefundedBookings = allOrgBookings.Count(b => b.Status == "Refunded"),
                TotalTicketsSold = allBookings.Sum(b => b.NumberOfTickets),
                TicketsSoldThisPeriod = periodBookings.Sum(b => b.NumberOfTickets),
                TicketsRefunded = allOrgBookings.Where(b => b.Status == "Refunded").Sum(b => b.NumberOfTickets),
                TicketsByTier = allBookings
                    .GroupBy(b => b.TicketTier.ToString())
                    .ToDictionary(g => g.Key, g => g.Sum(b => b.NumberOfTickets)),
                BookingConversionRate = allOrgBookings.Any() ?
                    (double)allBookings.Count / allOrgBookings.Count * 100 : 0
            };
        }

        private OrganizerVenues BuildVenues(
            List<Core.Models.Venue> venues,
            List<Core.Models.EventModels.EventBase> events)
        {
            var venueUsage = events
                .GroupBy(e => e.VenueId)
                .ToDictionary(g => g.Key, g => g.Count());

            var mostUsedVenue = venueUsage.OrderByDescending(v => v.Value).FirstOrDefault();
            var mostUsedVenueData = mostUsedVenue.Key > 0 ?
                venues.FirstOrDefault(v => v.VenueId == mostUsedVenue.Key) : null;

            return new OrganizerVenues
            {
                TotalVenues = venues.Count,
                ApprovedVenues = venues.Count(v => v.ApprovalStatus == ApprovalStatus.Approved),
                PendingVenues = venues.Count(v => v.ApprovalStatus == ApprovalStatus.Pending),
                RejectedVenues = venues.Count(v => v.ApprovalStatus == ApprovalStatus.Rejected),
                CurrentlyOccupiedVenues = venues.Count(v => v.IsCurrentlyOccupied()),
                MostUsedVenueName = mostUsedVenueData?.Name,
                MostUsedVenueEventCount = mostUsedVenue.Value
            };
        }

        private List<TimeSeriesData> BuildRevenueTimeSeries(
            List<Core.Models.BookingModels.Booking> bookings,
            DateFilter filter)
        {
            return bookings
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
        }

        private List<TimeSeriesData> BuildTicketsTimeSeries(
            List<Core.Models.BookingModels.Booking> bookings,
            DateFilter filter)
        {
            return bookings
                .Where(b => b.BookingDate >= filter.StartDate && b.BookingDate <= filter.EndDate)
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
        }

        private List<TimeSeriesData> BuildBookingsTimeSeries(
            List<Core.Models.BookingModels.Booking> bookings,
            DateFilter filter)
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

        private List<EventPerformanceData> BuildEventPerformance(
            List<Core.Models.EventModels.EventBase> events,
            List<Core.Models.BookingModels.Booking> bookings)
        {
            return bookings
                .GroupBy(b => new { b.EventId, b.Event!.Title, b.Event.Category, b.Event.StartDate })
                .Select(g => new EventPerformanceData
                {
                    EventId = g.Key.EventId,
                    EventTitle = g.Key.Title ?? "Unknown",
                    Category = g.Key.Category ?? "Unknown",
                    StartDate = g.Key.StartDate,
                    TotalRevenue = g.Sum(b => b.TotalWithTax),
                    TicketsSold = g.Sum(b => b.NumberOfTickets),
                    TotalBookings = g.Count(),
                    OccupancyRate = CalculateOccupancyRate(g.Key.EventId, g.Sum(b => b.NumberOfTickets))
                })
                .OrderByDescending(e => e.TotalRevenue)
                .ToList();
        }

        private double CalculateOccupancyRate(int eventId, int ticketsSold)
        {
            var evt = _unitOfWork.Events.GetById(eventId);
            if (evt?.Venue == null) return 0;

            return evt.Venue.Capacity > 0 ? (double)ticketsSold / evt.Venue.Capacity * 100 : 0;
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

        public async Task<byte[]> ExportFullReportAsync(int organizerId, DateFilter filter, string language = "en")
        {
            var stats = await GetDashboardStatisticsAsync(organizerId, filter);
            var organizer = _unitOfWork.Users.GetById(organizerId);
            var isArabic = language == "ar";

            using var workbook = new XLWorkbook();

            // Note: RTL is set at worksheet level in AddSectionHeader method

            // Overview Sheet
            AddOrganizerOverviewSheet(workbook, stats, organizer, filter, isArabic);

            // Financial Sheet
            AddOrganizerFinancialSheet(workbook, stats, isArabic);

            // Events Sheet
            AddOrganizerEventsSheet(workbook, stats, isArabic);

            // Bookings Sheet
            AddOrganizerBookingsSheet(workbook, stats, isArabic);

            // Event Performance Sheet
            AddEventPerformanceSheet(workbook, stats, isArabic);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportMonthlyReportAsync(int organizerId, int year, int month, string language = "en")
        {
            var filter = new DateFilter
            {
                Period = FilterPeriod.Custom,
                StartDate = new DateTime(year, month, 1),
                EndDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59)
            };

            return await ExportFullReportAsync(organizerId, filter, language);
        }

        private void AddOrganizerOverviewSheet(
            XLWorkbook workbook,
            OrganizerDashboardStatistics stats,
            Core.Models.User? organizer,
            DateFilter filter,
            bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "نظرة عامة" : "Overview");

            ws.Cell("A1").Value = isArabic ? "تقرير لوحة المنظم" : "Organizer Dashboard Report";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Range("A1:D1").Merge();

            ws.Cell("A2").Value = isArabic ? $"المنظم: {organizer?.FirstName} {organizer?.LastName}"
                : $"Organizer: {organizer?.FirstName} {organizer?.LastName}";
            ws.Range("A2:D2").Merge();

            ws.Cell("A3").Value = isArabic ? $"الفترة: {filter.StartDate:yyyy-MM-dd} إلى {filter.EndDate:yyyy-MM-dd}"
                : $"Period: {filter.StartDate:yyyy-MM-dd} to {filter.EndDate:yyyy-MM-dd}";
            ws.Range("A3:D3").Merge();

            int row = 5;

            AddSectionHeader(ws, row++, isArabic ? "ملخص" : "Summary", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الفعاليات" : "Total Events", stats.Overview.TotalEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات الموافق عليها" : "Approved Events", stats.Overview.ApprovedEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الأماكن" : "Total Venues", stats.Overview.TotalVenues, isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الإيرادات" : "Total Revenue", stats.Overview.TotalRevenue.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي التذاكر المباعة" : "Total Tickets Sold", stats.Overview.TotalTicketsSold, isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "هذه الفترة" : "This Period", isArabic);
            AddDataRow(ws, row++, isArabic ? "الإيرادات" : "Revenue", stats.Overview.RevenueThisPeriod.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "التذاكر المباعة" : "Tickets Sold", stats.Overview.TicketsSoldThisPeriod, isArabic);

            ws.Columns().AdjustToContents();
        }

        private void AddOrganizerFinancialSheet(XLWorkbook workbook, OrganizerDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "التفاصيل المالية" : "Financial Details");

            ws.Cell("A1").Value = isArabic ? "التقرير المالي" : "Financial Report";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "الأرقام المالية الرئيسية" : "Key Financial Figures", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الإيرادات" : "Gross Revenue", stats.Financials.GrossRevenue.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الضرائب المحصلة" : "Tax Collected", stats.Financials.TotalTaxCollected.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "الخصومات" : "Discounts", stats.Financials.TotalDiscounts.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "المبالغ المستردة" : "Refunded", stats.Financials.TotalRefunded.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "التذاكر المستردة" : "Tickets Refunded", stats.Financials.TotalRefundedTickets, isArabic);
            AddDataRow(ws, row++, isArabic ? "صافي الربح" : "Net Profit", stats.Financials.NetProfit.ToString("N2"), isArabic, true);

            row += 2;

            AddSectionHeader(ws, row++, isArabic ? "متوسطات" : "Averages", isArabic);
            AddDataRow(ws, row++, isArabic ? "متوسط الإيرادات لكل فعالية" : "Avg Revenue per Event", stats.Financials.AverageRevenuePerEvent.ToString("N2"), isArabic);
            AddDataRow(ws, row++, isArabic ? "متوسط سعر التذكرة" : "Avg Ticket Price", stats.Financials.AverageTicketPrice.ToString("N2"), isArabic);

            ws.Columns().AdjustToContents();
        }

        private void AddOrganizerEventsSheet(XLWorkbook workbook, OrganizerDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "الفعاليات" : "Events");

            ws.Cell("A1").Value = isArabic ? "تحليل الفعاليات" : "Events Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الفعاليات" : "Event Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الفعاليات" : "Total Events", stats.Events.TotalEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات القادمة" : "Upcoming Events", stats.Events.UpcomingEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات الجارية" : "Ongoing Events", stats.Events.OngoingEvents, isArabic);
            AddDataRow(ws, row++, isArabic ? "الفعاليات المكتملة" : "Completed Events", stats.Events.CompletedEvents, isArabic);

            row += 2;

            AddSectionHeader(ws, row++, isArabic ? "الفعاليات حسب الفئة" : "Events by Category", isArabic);
            foreach (var cat in stats.Events.EventsByCategory)
            {
                AddDataRow(ws, row++, cat.Key, cat.Value.ToString(), isArabic);
            }

            ws.Columns().AdjustToContents();
        }

        private void AddOrganizerBookingsSheet(XLWorkbook workbook, OrganizerDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "الحجوزات" : "Bookings");

            ws.Cell("A1").Value = isArabic ? "تحليل الحجوزات" : "Bookings Analysis";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            AddSectionHeader(ws, row++, isArabic ? "إحصائيات الحجوزات" : "Booking Statistics", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي الحجوزات" : "Total Bookings", stats.Bookings.TotalBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات المؤكدة" : "Confirmed Bookings", stats.Bookings.ConfirmedBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "الحجوزات المستردة" : "Refunded Bookings", stats.Bookings.RefundedBookings, isArabic);
            AddDataRow(ws, row++, isArabic ? "معدل التحويل" : "Conversion Rate", $"{stats.Bookings.BookingConversionRate:F2}%", isArabic);

            row++;

            AddSectionHeader(ws, row++, isArabic ? "التذاكر" : "Tickets", isArabic);
            AddDataRow(ws, row++, isArabic ? "إجمالي التذاكر المباعة" : "Total Tickets Sold", stats.Bookings.TotalTicketsSold, isArabic);
            AddDataRow(ws, row++, isArabic ? "التذاكر المستردة" : "Tickets Refunded", stats.Bookings.TicketsRefunded, isArabic);

            row += 2;

            AddSectionHeader(ws, row++, isArabic ? "التذاكر حسب الفئة" : "Tickets by Tier", isArabic);
            foreach (var tier in stats.Bookings.TicketsByTier)
            {
                AddDataRow(ws, row++, tier.Key, tier.Value.ToString(), isArabic);
            }

            ws.Columns().AdjustToContents();
        }

        private void AddEventPerformanceSheet(XLWorkbook workbook, OrganizerDashboardStatistics stats, bool isArabic)
        {
            var ws = workbook.Worksheets.Add(isArabic ? "أداء الفعاليات" : "Event Performance");

            ws.Cell("A1").Value = isArabic ? "أداء الفعاليات" : "Event Performance";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;

            int row = 3;

            ws.Cell(row, 1).Value = isArabic ? "عنوان الفعالية" : "Event Title";
            ws.Cell(row, 2).Value = isArabic ? "الفئة" : "Category";
            ws.Cell(row, 3).Value = isArabic ? "الإيرادات" : "Revenue";
            ws.Cell(row, 4).Value = isArabic ? "التذاكر المباعة" : "Tickets Sold";
            ws.Cell(row, 5).Value = isArabic ? "معدل الإشغال" : "Occupancy %";
            ws.Range(row, 1, row, 5).Style.Font.Bold = true;
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var evt in stats.TopPerformingEvents)
            {
                ws.Cell(row, 1).Value = evt.EventTitle;
                ws.Cell(row, 2).Value = evt.Category;
                ws.Cell(row, 3).Value = evt.TotalRevenue;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Value = evt.TicketsSold;
                ws.Cell(row, 5).Value = $"{evt.OccupancyRate:F2}%";
                row++;
            }

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