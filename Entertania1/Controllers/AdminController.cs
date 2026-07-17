using Core.Interfaces.ServicesInterface;
using Entertania1.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services;
using System.Security.Claims;


namespace Entertania1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AdminController> _logger;
        private readonly IRefundService _refundService;

        public AdminController(
            IAdminService adminService,
            IFileStorageService fileStorageService,
            ILogger<AdminController> logger,
            IRefundService refundService)
        {
            _adminService = adminService;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _refundService = refundService;
        }

        // -------------------- DASHBOARD --------------------
        [Route("Admin/Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get domain data from service
                var domainData = await _adminService.GetDashboardDataAsync();
                
                // Map to ViewModel (this is the controller's job!)
                var viewModel = new AdminDashboardViewModel
                {
                    PendingOrganizers = domainData.PendingOrganizers,
                    PendingVenues = domainData.PendingVenues,
                    PendingEvents = domainData.PendingEvents,
                    PendingRefunds = domainData.PendingRefunds,
                    EventStats = new EventStatistics
                    {
                        TotalEvents = domainData.EventStats.TotalEvents,
                        UpcomingEvents = domainData.EventStats.UpcomingEvents,
                        CurrentEvents = domainData.EventStats.CurrentEvents,
                        EndedEvents = domainData.EventStats.EndedEvents,
                        PendingApproval = domainData.EventStats.PendingApproval,
                        ApprovedEvents = domainData.EventStats.ApprovedEvents,
                        RejectedEvents = domainData.EventStats.RejectedEvents,
                        UpcomingEventsList = domainData.EventStats.UpcomingEventsList,
                        CurrentEventsList = domainData.EventStats.CurrentEventsList,
                        RecentlyEndedEvents = domainData.EventStats.RecentlyEndedEvents
                    },
                    VenueOccupancy = domainData.VenueOccupancy.Select(v => new VenueOccupancyInfo
                    {
                        VenueId = v.VenueId,
                        VenueName = v.VenueName,
                        Location = v.Location,
                        Capacity = v.Capacity,
                        IsCurrentlyOccupied = v.IsCurrentlyOccupied,
                        CurrentEvent = v.CurrentEvent,
                        TotalEventsScheduled = v.TotalEventsScheduled
                    }).ToList(),
                    RevenueStats = new RevenueStatistics
                    {
                        TotalRevenue = domainData.RevenueStats.TotalRevenue,
                        NetRevenue = domainData.RevenueStats.NetRevenue,
                        TotalTaxCollected = domainData.RevenueStats.TotalTaxCollected,
                        TotalDiscountsGiven = domainData.RevenueStats.TotalDiscountsGiven,
                        TotalRefunded = domainData.RevenueStats.TotalRefunded,
                        RevenueByEvent = domainData.RevenueStats.RevenueByEvent,
                        RevenueByCurrency = domainData.RevenueStats.RevenueByCurrency,
                        BookingsByEvent = domainData.RevenueStats.BookingsByEvent
                    },
                    PaymentStats = new PaymentStatistics
                    {
                        TotalPayments = domainData.PaymentStats.TotalPayments,
                        CompletedPayments = domainData.PaymentStats.CompletedPayments,
                        PendingPayments = domainData.PaymentStats.PendingPayments,
                        FailedPayments = domainData.PaymentStats.FailedPayments,
                        RefundedPayments = domainData.PaymentStats.RefundedPayments,
                        TotalAmountProcessed = domainData.PaymentStats.TotalAmountProcessed,
                        TotalAmountRefunded = domainData.PaymentStats.TotalAmountRefunded,
                        SuccessRate = domainData.PaymentStats.SuccessRate
                    }
                };

                return View("Dashboard", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["Error"] = "Error loading dashboard";
                return View("Dashboard", new AdminDashboardViewModel());
            }
        }

        // -------------------- ORGANIZER DOCUMENTS VIEW --------------------
        [HttpGet]
        public async Task<IActionResult> GetOrganizerDocuments(int organizerId)
        {
            try
            {
                var docs = await _adminService.GetOrganizerDocumentsAsync(organizerId);
                
                if (docs.Count == 0)
                {
                    return Json(new List<object>());
                }

                // Map to anonymous objects for JSON response
                var result = docs.Select(d => new
                {
                    verificationDocId = d.VerificationDocId,
                    fileName = d.FileName,
                    filePath = d.FilePath,
                    size = _fileStorageService.FormatFileSize(d.FileSize),
                    uploadedAt = d.UploadedAt
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching organizer documents for organizer {organizerId}");
                return Json(new { error = ex.Message });
            }
        }

        /// <summary>
        /// View/Preview verification file (inline display)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewVerificationFile(int docId)
        {
            try
            {
                var (success, doc, errorMessage) = await _adminService.GetVerificationDocumentAsync(docId);

                if (!success || doc == null)
                {
                    return NotFound(errorMessage);
                }

                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(webRootPath, filePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("File not found on server.");
                }

                var contentType = doc.ContentType ?? _fileStorageService.GetContentType(doc.FileName);
                return PhysicalFile(fullPath, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error viewing verification file {docId}");
                return StatusCode(500, "Error retrieving file.");
            }
        }

        /// <summary>
        /// Download verification file (force download)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DownloadVerificationFile(int docId)
        {
            try
            {
                var (success, doc, errorMessage) = await _adminService.GetVerificationDocumentAsync(docId);

                if (!success || doc == null)
                {
                    return NotFound(errorMessage);
                }

                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var filePath = doc.FilePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(webRootPath, filePath);

                if (!System.IO.File.Exists(fullPath))
                {
                    return NotFound("File not found on server.");
                }

                var contentType = doc.ContentType ?? _fileStorageService.GetContentType(doc.FileName);
                return PhysicalFile(fullPath, contentType, doc.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading verification file {docId}");
                return StatusCode(500, "Error downloading file.");
            }
        }

        // -------------------- REFUND ACTIONS --------------------
        // ===================================================================
        // ✅ NEW METHODS - Add these to your existing AdminController
        // ===================================================================

        /// <summary>
        /// GET: Admin/RefundRequests - View all pending refund requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RefundRequests()
        {
            try
            {
                var pendingRefunds = await _refundService.GetPendingRefundsAsync();

                // Sort by request date (oldest first for fairness)
                var sortedRefunds = pendingRefunds.OrderBy(r => r.RequestedAt).ToList();

                return View(sortedRefunds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading refund requests");
                TempData["Error"] = "Failed to load refund requests.";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// GET: Admin/RefundDetails/{id} - View detailed refund information
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RefundDetails(int id)
        {
            try
            {
                var refund = await _refundService.GetRefundByIdAsync(id);

                if (refund == null)
                {
                    TempData["Error"] = "Refund request not found.";
                    return RedirectToAction(nameof(RefundRequests));
                }

                return View(refund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading refund details for ID {id}");
                TempData["Error"] = "Failed to load refund details.";
                return RedirectToAction(nameof(RefundRequests));
            }
        }

        /// <summary>
        /// POST: Admin/ApproveRefund - Approve and process refund via Paymob
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRefund(int refundId, string? adminNote)
        {
            try
            {
                var currentUserId = GetCurrentUserId(); // Uses existing method

                _logger.LogInformation($"Admin {currentUserId} attempting to approve refund {refundId}");

                var result = await _refundService.ApproveRefundAsync(refundId, currentUserId, adminNote);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    _logger.LogInformation($"✅ Admin {currentUserId} approved refund {refundId}");
                }
                else
                {
                    TempData["Error"] = result.Message;
                    _logger.LogWarning($"❌ Admin {currentUserId} failed to approve refund {refundId}: {result.Message}");
                }

                return RedirectToAction(nameof(RefundRequests));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving refund {refundId}");
                TempData["Error"] = "An error occurred while processing the refund.";
                return RedirectToAction(nameof(RefundRequests));
            }
        }

        /// <summary>
        /// POST: Admin/RejectRefund - Reject refund request
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRefund(int refundId, string rejectionReason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["Error"] = "Please provide a reason for rejection.";
                    return RedirectToAction(nameof(RefundRequests)); // ✅ FIXED: Redirect to RefundRequests
                }

                var currentUserId = GetCurrentUserId();

                _logger.LogInformation($"Admin {currentUserId} attempting to reject refund {refundId}");

                var result = await _refundService.RejectRefundAsync(refundId, currentUserId, rejectionReason);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    _logger.LogInformation($"✅ Admin {currentUserId} rejected refund {refundId}");
                }
                else
                {
                    TempData["Error"] = result.Message;
                    _logger.LogWarning($"❌ Admin {currentUserId} failed to reject refund {refundId}: {result.Message}");
                }

                return RedirectToAction(nameof(RefundRequests)); // ✅ ALWAYS redirect here
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting refund {refundId}");
                TempData["Error"] = "An error occurred while rejecting the refund.";
                return RedirectToAction(nameof(RefundRequests)); // ✅ ALWAYS redirect here
            }
        }

        ///// <summary>
        ///// Helper method to get current admin user ID
        ///// </summary>
        //private int GetCurrentUserId()
        //{
        //    var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        //    return int.TryParse(idClaim, out var id) ? id : 0;
        //}

        // -------------------- ORGANIZER ACTIONS --------------------
        [HttpPost]
        public async Task<IActionResult> ApproveOrganizer(int id, string tab)
        {
            try
            {
                var (success, message) = await _adminService.ApproveOrganizerAsync(id);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving organizer {id}");
                TempData["Error"] = "An error occurred while approving the organizer";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrganizer(int id, string tab)
        {
            try
            {
                var (success, message) = await _adminService.RejectOrganizerAsync(id);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting organizer {id}");
                TempData["Error"] = "An error occurred while rejecting the organizer";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        // -------------------- VENUE ACTIONS --------------------
        [HttpPost]
        public async Task<IActionResult> ApproveVenue(int id, string tab)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (success, message) = await _adminService.ApproveVenueAsync(id, adminId);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving venue {id}");
                TempData["Error"] = "An error occurred while approving the venue";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        [HttpPost]
        public async Task<IActionResult> RejectVenue(int id, string tab)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (success, message) = await _adminService.RejectVenueAsync(id, adminId);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting venue {id}");
                TempData["Error"] = "An error occurred while rejecting the venue";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        // -------------------- EVENT ACTIONS --------------------
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id, string tab)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (success, message) = await _adminService.ApproveEventAsync(id, adminId);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving event {id}");
                TempData["Error"] = "An error occurred while approving the event";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id, string tab)
        {
            try
            {
                var adminId = GetCurrentUserId();
                var (success, message) = await _adminService.RejectEventAsync(id, adminId);
                TempData[success ? "Success" : "Error"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting event {id}");
                TempData["Error"] = "An error occurred while rejecting the event";
            }

            return RedirectToAction("Dashboard", new { tab });
        }

        // -------------------- HELPER METHODS --------------------
        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idClaim, out var currentUserId);
            return currentUserId;
        }
    }
}