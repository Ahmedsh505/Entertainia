using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Entertania1.ViewModels.Organizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrganizerDashboardService _dashboardService;
        private readonly IUserService _userService;
        private readonly ILogger<OrganizerController> _logger;

        public OrganizerController(
            IUnitOfWork unitOfWork,
            IOrganizerDashboardService dashboardService,
            IUserService userService,
            ILogger<OrganizerController> logger)
        {
            _unitOfWork = unitOfWork;
            _dashboardService = dashboardService;
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Main Dashboard View - FIXED: Now supports GET with query parameters
        /// </summary>
        [Authorize(Policy = "ActiveOrganizerOrAdmin")]
        [HttpGet]
        public async Task<IActionResult> Dashboard(string period = "last6months", string startDate = null, string endDate = null)
        {
            try
            {
                var organizerId = GetCurrentUserId();

                if (organizerId == 0)
                {
                    _logger.LogWarning("Invalid user ID for dashboard");
                    return RedirectToAction("Login", "Account");
                }

                // Check if organizer exists
                var organizer = await _userService.GetUserByIdAsync(organizerId);
                if (organizer == null)
                {
                    _logger.LogWarning($"Organizer not found: {organizerId}");
                    return RedirectToAction("Login", "Account");
                }

                if (organizer.IsDeleted == true)
                {
                    TempData["Warning"] = "Your account is deactivated. " +
                        "You can view past data but cannot create new events. " +
                        "Contact support to reactivate.";
                }

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
                            return RedirectToAction("Dashboard", new { period = "last6months" });
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

                _logger.LogInformation($"Loading dashboard for organizer {organizerId}, period: {period}, dates: {filter.StartDate} to {filter.EndDate}");

                var stats = await _dashboardService.GetDashboardStatisticsAsync(organizerId, filter);

                if (stats == null)
                {
                    _logger.LogError($"GetDashboardStatisticsAsync returned null for organizer {organizerId}");
                    throw new InvalidOperationException("Failed to load dashboard statistics");
                }

                var viewModel = new OrganizerDashboardStatsViewModel
                {
                    Statistics = stats,
                    SelectedPeriod = period,
                    FilterStartDate = filter.StartDate,
                    FilterEndDate = filter.EndDate,
                    OrganizerName = $"{organizer.FirstName} {organizer.LastName}"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer dashboard");
                TempData["Error"] = $"Error loading dashboard: {ex.Message}";

                // Return view with empty model to prevent blank page
                var emptyViewModel = new OrganizerDashboardStatsViewModel
                {
                    SelectedPeriod = period ?? "last6months",
                    FilterStartDate = DateTime.Today.AddMonths(-6),
                    FilterEndDate = DateTime.Today,
                    OrganizerName = "User"
                };

                return View(emptyViewModel);
            }
        }

        /// <summary>
        /// Custom Date Range Filter - FIXED: Now uses GET with redirect
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
                    return RedirectToAction("Dashboard");
                }

                if (!DateTime.TryParse(endDate, out DateTime parsedEndDate))
                {
                    TempData["Error"] = "Invalid end date format";
                    return RedirectToAction("Dashboard");
                }

                // Validate dates
                if (parsedStartDate > parsedEndDate)
                {
                    TempData["Error"] = "Start date cannot be after end date";
                    return RedirectToAction("Dashboard");
                }

                if (parsedEndDate > DateTime.Today)
                {
                    TempData["Error"] = "End date cannot be in the future";
                    return RedirectToAction("Dashboard");
                }

                // Redirect to Dashboard with query parameters
                return RedirectToAction("Dashboard", new
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
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Download Full Report - FIXED: Added date range support
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadReport(string period = "alltime", string language = "en", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var organizerId = GetCurrentUserId();

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

                var excelData = await _dashboardService.ExportFullReportAsync(organizerId, filter, language);

                var fileName = $"Organizer_Report_{filter.StartDate:yyyy_MM_dd}_to_{filter.EndDate:yyyy_MM_dd}.xlsx";
                return File(excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading organizer report");
                TempData["Error"] = "Error generating report";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Download Monthly Report
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadMonthlyReport(int? year = null, int? month = null, string language = "en")
        {
            try
            {
                var organizerId = GetCurrentUserId();
                var targetYear = year ?? DateTime.UtcNow.Year;
                var targetMonth = month ?? DateTime.UtcNow.Month;

                var excelData = await _dashboardService.ExportMonthlyReportAsync(
                    organizerId,
                    targetYear,
                    targetMonth,
                    language);

                var fileName = $"Organizer_Monthly_Report_{targetYear}_{targetMonth:D2}.xlsx";
                return File(excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading monthly report");
                TempData["Error"] = "Error generating monthly report";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// API endpoint for chart data - FIXED: Added date range support
        /// </summary>
        [HttpGet]
        [Route("Organizer/api/charts/{chartType}")]
        public async Task<IActionResult> GetChartData(string chartType, string period = "last6months", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var organizerId = GetCurrentUserId();

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

                var stats = await _dashboardService.GetDashboardStatisticsAsync(organizerId, filter);

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

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var userId) ? userId : 0;
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
                _ => DateFilter.Last6Months
            };
        }
    }
}