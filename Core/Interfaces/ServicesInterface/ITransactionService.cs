using Core.Enums;
using Core.Models.PaymentModels;

namespace Core.Interfaces.ServicesInterface
{
    public interface ITransactionService
    {
        // ===== LOGGING TRANSACTIONS =====
        Task<Transaction> LogPaymentAsync(
            int userId,
            int bookingId,
            int paymentId,
            decimal amount,
            Currency currency,
            PaymentStatus status,
            string? paymentMethod = null,
            string? transactionReference = null,
            string? gatewayTransactionId = null);

        Task<Transaction> LogRefundAsync(
            int userId,
            int bookingId,
            int refundId,
            decimal amount,
            Currency currency,
            PaymentStatus status,
            string? transactionReference = null,
            string? gatewayTransactionId = null);

        Task<Transaction> LogCancellationAsync(
            int userId,
            int bookingId,
            string reason);

        // ===== UPDATE TRANSACTIONS =====
        Task UpdateTransactionStatusAsync(
            int transactionId,
            PaymentStatus newStatus,
            string? message = null);

        Task UpdateTransactionByReferenceAsync(
            string transactionReference,
            PaymentStatus newStatus,
            string? gatewayTransactionId = null,
            string? message = null);

        // ===== RETRIEVE TRANSACTIONS =====
        Task<Transaction?> GetTransactionByIdAsync(int transactionId);
        Task<Transaction?> GetTransactionByReferenceAsync(string transactionReference);
        Task<List<Transaction>> GetUserTransactionsAsync(int userId);
        Task<List<Transaction>> GetBookingTransactionsAsync(int bookingId);
    }
}
