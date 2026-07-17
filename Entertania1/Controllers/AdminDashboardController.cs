using Core.Interfaces.ServicesInterface;
using Entertania1.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    /// <summary>
    /// Admin Dashboard Controller - Statistics and Analytics Only
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IAdminDashboardService _dashboardService;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IAdminDashboardService dashboardService,
            ILogger<AdminDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Main Dashboard View - FIXED: Removed route attribute that was causing issues
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string period = "last30days", string startDate = null, string endDate = null)
        {
            try
            {
                DateFilter filter;

                // If custom dates provided as strings, parse them
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    if (DateTime.TryParse(startDate, out DateTime parsedStart) && DateTime.TryParse(endDate, out DateTime parsedEnd))
                    {
                        // Validate date range
                        if (parsedStart > parsedEnd)
                        {
                            TempData["Error"] = "Start date cannot be after end date";
                            return RedirectToAction("Index", new { period = "last30days" });
                        }

                        filter = new DateFilter
                        {
                            Period = FilterPeriod.Custom,
                            StartDate = parsedStart,
                            EndDate = parsedEnd.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                        };
                        period = "custom";
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid date format - Start: {startDate}, End: {endDate}");
                        filter = GetDateFilter(period);
                    }
                }
                else
                {
                    filter = GetDateFilter(period);
                }

                _logger.LogInformation($"Loading admin dashboard, period: {period}, dates: {filter.StartDate} to {filter.EndDate}");

                var stats = await _dashboardService.GetDashboardStatisticsAsync(filter);

                if (stats == null)
                {
                    _logger.LogError("GetDashboardStatisticsAsync returned null");
                    throw new InvalidOperationException("Failed to load dashboard statistics");
                }

                var viewModel = new AdminDashboardStatsViewModel
                {
                    Statistics = stats,
                    SelectedPeriod = period,
                    FilterStartDate = filter.StartDate,
                    FilterEndDate = filter.EndDate
                };

                return View("Statistics", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = $"Error loading dashboard statistics: {ex.Message}";

                // Return view with empty model
                var emptyViewModel = new AdminDashboardStatsViewModel
                {
                    SelectedPeriod = period ?? "last30days",
                    FilterStartDate = DateTime.Today.AddDays(-30),
                    FilterEndDate = DateTime.Today
                };

                return View("Statistics", emptyViewModel);
            }
        }

        /// <summary>
        /// Custom Date Range Filter - FIXED: Now uses GET with query parameters
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FilterByDateRange(string startDate, string endDate)
        {
            try
            {
                // Parse dates from string
                if (!DateTime.TryParse(startDate, out DateTime parsedStartDate))
                {
                    TempData["Error"] = "Invalid start date format";
                    return RedirectToAction("Index");
                }

                if (!DateTime.TryParse(endDate, out DateTime parsedEndDate))
                {
                    TempData["Error"] = "Invalid end date format";
                    return RedirectToAction("Index");
                }

                // Validate dates
                if (parsedStartDate > parsedEndDate)
                {
                    TempData["Error"] = "Start date cannot be after end date";
                    return RedirectToAction("Index");
                }

                if (parsedEndDate > DateTime.Today)
                {
                    TempData["Error"] = "End date cannot be in the future";
                    return RedirectToAction("Index");
                }

                // Redirect to Index with query parameters
                return RedirectToAction("Index", new
                {
                    period = "custom",
                    startDate = parsedStartDate.ToString("yyyy-MM-dd"),
                    endDate = parsedEndDate.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering dashboard");
                TempData["Error"] = "Error applying date filter";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Export Full Report to Excel
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportReport(string period = "last30days", string language = "en", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                DateFilter filter;

                // Handle custom date range for export
                if (period == "custom" && startDate.HasValue && endDate.HasValue)
                {
                    filter = new DateFilter
                    {
                        Period = FilterPeriod.Custom,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                    };
                }
                else
                {
                    filter = GetDateFilter(period);
                }

                var excelData = await _dashboardService.ExportFullReportAsync(filter, language);

                var fileName = $"Admin_Dashboard_Report_{filter.StartDate:yyyy_MM_dd}_to_{filter.EndDate:yyyy_MM_dd}.xlsx";
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting admin dashboard report");
                TempData["Error"] = "Error exporting report";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// API endpoint for chart data - FIXED: Added startDate/endDate support
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType, string period = "last30days", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                DateFilter filter;

                if (period == "custom" && startDate.HasValue && endDate.HasValue)
                {
                    filter = new DateFilter
                    {
                        Period = FilterPeriod.Custom,
                        StartDate = startDate.Value,
                        EndDate = endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59)
                    };
                }
                else
                {
                    filter = GetDateFilter(period);
                }

                var stats = await _dashboardService.GetDashboardStatisticsAsync(filter);

                return chartType.ToLower() switch
                {
                    "revenue" => Json(new
                    {
                        labels = stats.RevenueTimeSeries.Select(t => t.Label),
                        data = stats.RevenueTimeSeries.Select(t => t.Value)
                    }),
                    "tickets" => Json(new
                    {
                        labels = stats.TicketsTimeSeries.Select(t => t.Label),
                        data = stats.TicketsTimeSeries.Select(t => t.Value)
                    }),
                    "bookings" => Json(new
                    {
                        labels = stats.BookingsTimeSeries.Select(t => t.Label),
                        data = stats.BookingsTimeSeries.Select(t => t.Value)
                    }),
                    _ => BadRequest("Invalid chart type")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chart data for {chartType}");
                return StatusCode(500, "Error loading chart data");
            }
        }

        private DateFilter GetDateFilter(string period)
        {
            return period.ToLower() switch
            {
                "last7days" => DateFilter.Last7Days,
                "last30days" => DateFilter.Last30Days,
                "last6months" => DateFilter.Last6Months,
                "lastyear" => DateFilter.LastYear,
                "last5years" => DateFilter.Last5Years,
                "alltime" => DateFilter.AllTime,
                _ => DateFilter.Last30Days
            };
        }
    }
}