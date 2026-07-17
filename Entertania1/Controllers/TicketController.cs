using Core.Interfaces.ServicesInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    public class TicketController : Controller
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(
            ITicketService ticketService,
            ILogger<TicketController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : 0;
        }

        // ===== PUBLIC TICKET VERIFICATION =====
        [HttpGet("Ticket/Verify/{ticketId}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(int ticketId, string? hash = null)
        {
            try
            {
                _logger.LogInformation($"🔍 Verifying ticket {ticketId} with hash: {hash?.Substring(0, Math.Min(10, hash?.Length ?? 0))}...");

                var ticket = await _ticketService.GetTicketByIdAsync(ticketId);

                if (ticket == null)
                {
                    _logger.LogWarning($"❌ Ticket {ticketId} not found in database");
                    ViewBag.ErrorMessage = "Ticket not found";
                    return View("TicketNotFound");
                }

                _logger.LogInformation($"✅ Found ticket {ticketId} in database. Status: {ticket.Status}");

                bool isValid = true;
                string message = "Ticket is valid ✓";

                if (!string.IsNullOrEmpty(hash))
                {
                    var decodedHash = Uri.UnescapeDataString(hash);

                    if (ticket.VerificationHash != decodedHash)
                    {
                        _logger.LogWarning($"❌ Hash mismatch for ticket {ticketId}");
                        _logger.LogWarning($"Expected: {ticket.VerificationHash?.Substring(0, Math.Min(20, ticket.VerificationHash?.Length ?? 0))}");
                        _logger.LogWarning($"Received: {decodedHash?.Substring(0, Math.Min(20, decodedHash?.Length ?? 0))}");
                        isValid = false;
                        message = "Invalid ticket - Verification failed";
                    }
                    else
                    {
                        _logger.LogInformation($"✅ Hash verification successful for ticket {ticketId}");
                    }
                }

                if (isValid)
                {
                    if (ticket.ExpiresAt.HasValue && ticket.ExpiresAt.Value < DateTime.UtcNow)
                    {
                        isValid = false;
                        message = "Ticket has expired";
                        _logger.LogWarning($"❌ Ticket {ticketId} has expired");
                    }

                    if (ticket.Status != "Active")
                    {
                        isValid = false;
                        message = $"Ticket is {ticket.Status.ToLower()}";
                        _logger.LogWarning($"❌ Ticket {ticketId} status is {ticket.Status}");
                    }
                }

                ViewBag.IsValid = isValid;
                ViewBag.Message = message;
                ViewBag.QRCodeBase64 = _ticketService.GenerateQRCodeBase64(ticket.QRCodeData);

                _logger.LogInformation($"Ticket {ticketId} verification result: {(isValid ? "VALID ✅" : "INVALID ❌")}");

                return View(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying ticket {ticketId}");
                ViewBag.ErrorMessage = "An error occurred while verifying the ticket";
                return View("TicketNotFound");
            }
        }

        // ===== SCAN TICKET (Venue Staff/Admin) =====
        [Authorize(Roles = "Admin,Organizer")]
        [HttpPost]
        public async Task<IActionResult> Scan(int ticketId)
        {
            var currentUserId = GetCurrentUserId();
            var (success, message) = await _ticketService.ScanTicketAsync(ticketId, currentUserId);

            if (success)
            {
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = message;
            }

            return RedirectToAction(nameof(Verify), new { ticketId });
        }

        // ===== DOWNLOAD TICKETS PDF =====
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadPDF(int bookingId)
        {
            try
            {
                var pdfBytes = await _ticketService.GenerateTicketsPDFAsync(bookingId);
                return File(pdfBytes, "application/pdf", $"Tickets_Booking_{bookingId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading tickets PDF for booking {bookingId}");
                TempData["Error"] = "Failed to generate tickets PDF";
                return RedirectToAction("Details", "Booking", new { bookingId });
            }
        }

        // ===== MY TICKETS =====
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyTickets()
        {
            var currentUserId = GetCurrentUserId();
            var tickets = await _ticketService.GetTicketsByUserIdAsync(currentUserId);

            return View(tickets);
        }

        // ===== ADMIN: VIEW EVENT TICKETS =====
        [Authorize(Roles = "Admin,Organizer")]
        [HttpGet]
        public async Task<IActionResult> EventTickets(int eventId)
        {
            var tickets = await _ticketService.GetTicketsByEventIdAsync(eventId);
            return View(tickets);
        }
    }
}