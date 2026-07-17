using Core.Interfaces.ServicesInterface;
using Entertania1.ViewModels.PaymentHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Entertania1.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class PaymentHistoryController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<PaymentHistoryController> _logger;

        public PaymentHistoryController(
            ITransactionService transactionService,
            ILogger<PaymentHistoryController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        }

        /// <summary>
        /// ✅ FIXED: Show payments AND refunds, grouped by booking, showing progression
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation($"Loading transaction history for user {userId}");

                // ✅ Get all transactions for this user
                var allTransactions = await _transactionService.GetUserTransactionsAsync(userId);

                // ✅ Group by booking to show progression
                var groupedByBooking = allTransactions
                    .OrderByDescending(t => t.CreatedAt)
                    .GroupBy(t => t.BookingId)
                    .Select(group =>
                    {
                        var transactions = group.OrderBy(t => t.CreatedAt).ToList();
                        var latestTransaction = transactions.Last();

                        return new PaymentHistoryVM
                        {
                            EventName = latestTransaction.Booking?.Event?.Title ?? "N/A",
                            Amount = latestTransaction.Amount,
                            Status = latestTransaction.Status,
                            PaymentMethod = latestTransaction.PaymentMethod ?? latestTransaction.PaymentGateway ?? "Paymob",
                            TransactionType = latestTransaction.TransactionType,
                            CreatedAt = latestTransaction.CreatedAt,
                            UpdatedAt = latestTransaction.UpdatedAt ?? latestTransaction.CreatedAt,
                            Description = latestTransaction.Description,
                            OriginalAmount = latestTransaction.Refund?.OriginalAmount,

                            // ✅ Show progression
                            StatusHistory = transactions.Select(t => new StatusHistoryItem
                            {
                                Status = t.Status,
                                Timestamp = t.UpdatedAt ?? t.CreatedAt,
                                Description = t.Description
                            }).ToList()
                        };
                    })
                    .ToList();

                _logger.LogInformation($"Found {groupedByBooking.Count} transaction groups for user {userId}");

                return View(groupedByBooking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment history");
                TempData["Error"] = "Failed to load payment history. Please try again.";
                return View(new List<PaymentHistoryVM>());
            }
        }
    }
}