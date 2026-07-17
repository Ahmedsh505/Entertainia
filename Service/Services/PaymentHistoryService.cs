using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Interfaces.ServicesInterface;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// ✅ COMPLETE FIX: properly finds and updates records using TransactionId
    /// </summary>
    public class PaymentHistoryService : IPaymentHistoryService
    {
        private readonly IPaymentHistoryRepository _paymentRepo;
        private readonly ILogger<PaymentHistoryService>? _logger;
        private readonly IEgyptTimeService _timeService;
        public PaymentHistoryService(
            IPaymentHistoryRepository paymentRepo,
            ILogger<PaymentHistoryService>? logger = null,
            IEgyptTimeService timeService = null)
        {
            _paymentRepo = paymentRepo;
            _logger = logger;
            _timeService = timeService;
        }

        public async Task LogAsync(
            int userId,
            int bookingId,
            decimal amount,
            PaymentStatus status,
            string? method = null,
            string? transactionId = null)
        {
            try
            {
                _logger?.LogInformation($"💳 Logging payment history: Booking {bookingId}, Status {status}, Amount {amount}, TxnId: {transactionId}");

                // Get all existing records for this booking (Tracked by EF Core)
                var existingRecords = await _paymentRepo.GetByBookingIdAsync(bookingId);
                var recordsList = existingRecords.ToList();

                PaymentHistory? recordToUpdate = null;

                // ========================================
                // 1️⃣ STRATEGY A: Match by Transaction ID (The most accurate method)
                // ========================================
                if (!string.IsNullOrEmpty(transactionId))
                {
                    // Finds specific Refund Requests (REFUND-REQ-XX) or Payment Transactions
                    recordToUpdate = recordsList.FirstOrDefault(p => p.TransactionId == transactionId);

                    if (recordToUpdate != null)
                    {
                        _logger?.LogInformation($"✅ Found exact record match by Transaction ID: {transactionId}");
                    }
                }

                // ========================================
                // 2️⃣ STRATEGY B: Fallback - Match by Logic (If ID missing or mismatched)
                // ========================================
                if (recordToUpdate == null)
                {
                    if (status == PaymentStatus.Refunded || status == PaymentStatus.Failed)
                    {
                        // REFUND FLOW: Look for the most recent PENDING refund request (Negative amount)
                        recordToUpdate = recordsList
                            .Where(p => p.Status == PaymentStatus.Pending && p.Amount <= 0)
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefault();
                    }
                    else if (amount > 0 && status != PaymentStatus.Pending)
                    {
                        // PAYMENT FLOW: Look for the most recent positive payment that isn't final yet
                        recordToUpdate = recordsList
                            .Where(p => p.Amount > 0 &&
                                        p.Status != PaymentStatus.Refunded &&
                                        p.Status != PaymentStatus.Failed &&
                                        p.Status != PaymentStatus.Completed)
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefault();
                    }
                }

                // ========================================
                // 3️⃣ EXECUTE: Update or Create
                // ========================================
                if (recordToUpdate != null)
                {
                    // ✅ UPDATE EXISTING RECORD
                    _logger?.LogInformation($"✅ Updating existing PaymentHistory ID {recordToUpdate.Id} to {status}");

                    recordToUpdate.Status = status;
                    recordToUpdate.Amount = amount; // Update amount (important for rejections setting to 0)
                    recordToUpdate.UpdatedAt = DateTime.UtcNow;

                    // Update metadata if provided
                    if (!string.IsNullOrEmpty(method)) recordToUpdate.PaymentMethod = method;
                    if (!string.IsNullOrEmpty(transactionId)) recordToUpdate.TransactionId = transactionId;

                    await _paymentRepo.UpdateAsync(recordToUpdate);
                    _logger?.LogInformation($"✅ Successfully updated record to {status}");
                }
                else
                {
                    // ➕ CREATE NEW RECORD
                    _logger?.LogInformation($"➕ No matching record found. Creating NEW history entry.");

                    var newRecord = new PaymentHistory
                    {
                        UserId = userId,
                        BookingId = bookingId,
                        Amount = amount,
                        Status = status,
                        PaymentMethod = method,
                        TransactionId = transactionId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _paymentRepo.AddAsync(newRecord);
                    _logger?.LogInformation($"✅ Successfully created new record");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"❌ Error logging payment history for booking {bookingId}");
                throw;
            }
        }

        public async Task<IEnumerable<PaymentHistory>> GetUserPaymentsAsync(int userId)
        {
            return await _paymentRepo.GetByUserIdAsync(userId);
        }
    }
}