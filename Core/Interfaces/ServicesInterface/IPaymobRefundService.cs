using Core.Models.PaymentModels;

namespace Core.Interfaces.ServicesInterface
{
    /// <summary>
    /// Service for handling Paymob refund API integration
    /// </summary>
    public interface IPaymobRefundService
    {
        /// <summary>
        /// Process a refund through Paymob API
        /// </summary>
        /// <param name="refund">The refund entity</param>
        /// <param name="payment">The original payment</param>
        /// <returns>Success status and message</returns>
        Task<(bool Success, string Message, string? PaymobTransactionId)> ProcessRefundAsync(
            Refund refund,
            Payment payment);

        /// <summary>
        /// Verify refund status from Paymob
        /// </summary>
        Task<(bool Success, string Status)> VerifyRefundStatusAsync(string paymobTransactionId);

        /// <summary>
        /// Handle Paymob refund webhook callback
        /// </summary>
        Task<bool> HandleRefundWebhookAsync(Dictionary<string, string> callbackData);
    }
}