using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.PaymentModels;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// ✅ COMPLETE FIX: Smart transaction logging that UPDATES instead of DUPLICATES
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionService> _logger;
        private readonly IEgyptTimeService _timeService;

        public TransactionService(
            IUnitOfWork unitOfWork,
            ILogger<TransactionService> logger,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _timeService = timeService;
            _timeService = timeService;
        }

        #region Log Transactions - SMART UPDATE LOGIC

        /// <summary>
        /// ✅ FIXED: Update existing payment transaction instead of creating duplicates
        /// </summary>
        public async Task<Transaction> LogPaymentAsync(
            int userId,
            int bookingId,
            int paymentId,
            decimal amount,
            Currency currency,
            PaymentStatus status,
            string? paymentMethod = null,
            string? transactionReference = null,
            string? gatewayTransactionId = null)
        {
            try
            {
                _logger.LogInformation($"💳 Logging payment: Booking {bookingId}, Status {status}");

                // ✅ STEP 1: Check if transaction already exists for this payment
                var existingTransaction = _unitOfWork.Transactions
                    .GetByBooking(bookingId)
                    .FirstOrDefault(t => t.TransactionType == "Payment" && t.PaymentId == paymentId);

                if (existingTransaction != null)
                {
                    // ✅ UPDATE existing transaction
                    _logger.LogInformation($"🔄 Updating existing transaction {existingTransaction.TransactionId}");

                    existingTransaction.Status = status;
                    existingTransaction.Amount = amount;
                    existingTransaction.UpdatedAt = DateTime.UtcNow;

                    if (status == PaymentStatus.Completed || status == PaymentStatus.Refunded)
                    {
                        existingTransaction.CompletedAt = DateTime.UtcNow;
                    }

                    if (!string.IsNullOrEmpty(paymentMethod))
                        existingTransaction.PaymentMethod = paymentMethod;

                    if (!string.IsNullOrEmpty(gatewayTransactionId))
                        existingTransaction.GatewayTransactionId = gatewayTransactionId;

                    existingTransaction.Description = $"Payment {status} for booking #{bookingId}";

                    _unitOfWork.Transactions.Update(existingTransaction);
                    _unitOfWork.Complete();

                    _logger.LogInformation($"✅ Transaction updated to {status}");
                    return existingTransaction;
                }

                // ✅ STEP 2: Create new transaction (first time)
                _logger.LogInformation($"➕ Creating new payment transaction");

                var transaction = new Transaction
                {
                    UserId = userId,
                    BookingId = bookingId,
                    PaymentId = paymentId,
                    TransactionType = "Payment",
                    Amount = amount,
                    Currency = currency,
                    Status = status,
                    PaymentMethod = paymentMethod,
                    PaymentGateway = "Paymob",
                    TransactionReference = transactionReference,
                    GatewayTransactionId = gatewayTransactionId,
                    Description = $"Payment {status} for booking #{bookingId}",
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Transactions.Add(transaction);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ New payment transaction created: ID {transaction.TransactionId}");
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error logging payment for booking {bookingId}");
                throw;
            }
        }

        /// <summary>
        /// ✅ FIXED: Update existing refund transaction instead of creating duplicates
        /// </summary>
        public async Task<Transaction> LogRefundAsync(
            int userId,
            int bookingId,
            int refundId,
            decimal amount,
            Currency currency,
            PaymentStatus status,
            string? transactionReference = null,
            string? gatewayTransactionId = null)
        {
            try
            {
                _logger.LogInformation($"💰 Logging refund: Booking {bookingId}, Status {status}");

                // ✅ STEP 1: Check if transaction already exists for this refund
                var existingTransaction = _unitOfWork.Transactions
                    .GetByBooking(bookingId)
                    .FirstOrDefault(t => t.TransactionType == "Refund" && t.RefundId == refundId);

                if (existingTransaction != null)
                {
                    // ✅ UPDATE existing transaction
                    _logger.LogInformation($"🔄 Updating existing refund transaction {existingTransaction.TransactionId}");

                    existingTransaction.Status = status;
                    existingTransaction.Amount = amount;
                    existingTransaction.UpdatedAt = DateTime.UtcNow;

                    if (status == PaymentStatus.Refunded || status == PaymentStatus.RefundRejected)
                    {
                        existingTransaction.CompletedAt = DateTime.UtcNow;
                    }

                    if (!string.IsNullOrEmpty(gatewayTransactionId))
                        existingTransaction.GatewayTransactionId = gatewayTransactionId;

                    existingTransaction.Description = $"Refund {status} for booking #{bookingId}";

                    _unitOfWork.Transactions.Update(existingTransaction);
                    _unitOfWork.Complete();

                    _logger.LogInformation($"✅ Refund transaction updated to {status}");
                    return existingTransaction;
                }

                // ✅ STEP 2: Create new transaction (first refund request)
                _logger.LogInformation($"➕ Creating new refund transaction");

                var transaction = new Transaction
                {
                    UserId = userId,
                    BookingId = bookingId,
                    RefundId = refundId,
                    TransactionType = "Refund",
                    Amount = amount,
                    Currency = currency,
                    Status = status,
                    PaymentGateway = "Paymob",
                    TransactionReference = transactionReference,
                    GatewayTransactionId = gatewayTransactionId,
                    Description = $"Refund {status} for booking #{bookingId}",
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Transactions.Add(transaction);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ New refund transaction created: ID {transaction.TransactionId}");
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error logging refund for booking {bookingId}");
                throw;
            }
        }

        /// <summary>
        /// ✅ Log booking cancellation (before payment)
        /// </summary>
        public async Task<Transaction> LogCancellationAsync(
            int userId,
            int bookingId,
            string reason)
        {
            try
            {
                _logger.LogInformation($"❌ Logging cancellation: Booking {bookingId}");

                var transaction = new Transaction
                {
                    UserId = userId,
                    BookingId = bookingId,
                    TransactionType = "Cancellation",
                    Amount = 0,
                    Currency = Currency.EGP,
                    Status = PaymentStatus.Failed,
                    Description = $"Booking cancelled: {reason}",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                _unitOfWork.Transactions.Add(transaction);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Cancellation logged: ID {transaction.TransactionId}");
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error logging cancellation for booking {bookingId}");
                throw;
            }
        }

        #endregion

        #region Update Transactions

        public async Task UpdateTransactionStatusAsync(
            int transactionId,
            PaymentStatus newStatus,
            string? message = null)
        {
            try
            {
                var transaction = _unitOfWork.Transactions.GetById(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning($"⚠️ Transaction {transactionId} not found");
                    return;
                }

                _logger.LogInformation($"🔄 Updating transaction {transactionId} to {newStatus}");

                transaction.UpdateStatus(newStatus, message);
                _unitOfWork.Transactions.Update(transaction);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Transaction updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating transaction {transactionId}");
                throw;
            }
        }

        public async Task UpdateTransactionByReferenceAsync(
            string transactionReference,
            PaymentStatus newStatus,
            string? gatewayTransactionId = null,
            string? message = null)
        {
            try
            {
                var transaction = _unitOfWork.Transactions.GetByReference(transactionReference);
                if (transaction == null)
                {
                    _logger.LogWarning($"⚠️ Transaction {transactionReference} not found");
                    return;
                }

                _logger.LogInformation($"🔄 Updating transaction by reference");

                transaction.UpdateStatus(newStatus, message);

                if (!string.IsNullOrEmpty(gatewayTransactionId))
                {
                    transaction.GatewayTransactionId = gatewayTransactionId;
                }

                _unitOfWork.Transactions.Update(transaction);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Transaction updated by reference");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error updating transaction by reference");
                throw;
            }
        }

        #endregion

        #region Retrieve Transactions

        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
        {
            return await Task.Run(() => _unitOfWork.Transactions.GetById(transactionId));
        }

        public async Task<Transaction?> GetTransactionByReferenceAsync(string transactionReference)
        {
            return await Task.Run(() => _unitOfWork.Transactions.GetByReference(transactionReference));
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(int userId)
        {
            return await Task.Run(() => _unitOfWork.Transactions.GetByUser(userId));
        }

        public async Task<List<Transaction>> GetBookingTransactionsAsync(int bookingId)
        {
            return await Task.Run(() => _unitOfWork.Transactions.GetByBooking(bookingId));
        }

        #endregion
    }
}