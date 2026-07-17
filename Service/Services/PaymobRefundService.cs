using Core.Interfaces.ServicesInterface;
using Core.Models.PaymentModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Service.Services
{
    /// <summary>
    /// ✅ NEW: Paymob Refund API Integration Service
    /// Handles all Paymob refund operations following their API documentation
    /// </summary>
    public class PaymobRefundService : IPaymobRefundService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymobRefundService> _logger;
        private readonly IEgyptTimeService _timeService;

        // Paymob Configuration
        private string PaymobApiKey => _configuration["Paymob:ApiKey"] ?? "";

        public PaymobRefundService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<PaymobRefundService> logger,
            IEgyptTimeService timeService)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _timeService = timeService;
        }

        #region Process Refund

        public async Task<(bool Success, string Message, string? PaymobTransactionId)> ProcessRefundAsync(
            Refund refund,
            Payment payment)
        {
            try
            {
                _logger.LogInformation("=== Starting Paymob Refund Process ===");
                _logger.LogInformation($"Refund ID: {refund.RefundId}");
                _logger.LogInformation($"Payment ID: {payment.PaymentId}");
                _logger.LogInformation($"Gateway Transaction ID: {payment.PaymentGatewayReference}");
                _logger.LogInformation($"Refund Amount: {refund.RefundAmount} {refund.Currency}");

                // Validate we have a Paymob transaction ID
                if (string.IsNullOrEmpty(payment.PaymentGatewayReference))
                {
                    _logger.LogError("❌ No Paymob transaction ID found in payment");
                    return (false, "Cannot process refund: No Paymob transaction ID found", null);
                }

                // STEP 1: Get Authentication Token
                _logger.LogInformation("Step 1: Getting auth token...");
                var authToken = await GetAuthTokenAsync();

                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.LogError("❌ Failed to get auth token");
                    return (false, "Failed to authenticate with payment gateway", null);
                }

                _logger.LogInformation($"✓ Auth token received");

                // STEP 2: Process Refund via Paymob API
                _logger.LogInformation("Step 2: Processing refund with Paymob...");

                var refundResult = await ProcessPaymobRefundAsync(
                    authToken,
                    payment.PaymentGatewayReference,
                    refund.RefundAmount,
                    refund.Currency.ToString()
                );

                if (!refundResult.Success)
                {
                    _logger.LogError($"❌ Paymob refund failed: {refundResult.Message}");
                    return (false, refundResult.Message, null);
                }

                _logger.LogInformation($"✅ Paymob refund processed successfully");
                _logger.LogInformation($"Paymob Refund Transaction ID: {refundResult.TransactionId}");
                _logger.LogInformation("=== Refund Process Complete ===");

                return (true, "Refund processed successfully. Funds will be returned in 1-14 business days.", refundResult.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing Paymob refund");
                return (false, $"Refund processing failed: {ex.Message}", null);
            }
        }

        #endregion

        #region Verify Refund Status

        public async Task<(bool Success, string Status)> VerifyRefundStatusAsync(string paymobTransactionId)
        {
            try
            {
                _logger.LogInformation($"Verifying refund status for transaction: {paymobTransactionId}");

                var authToken = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(authToken))
                {
                    return (false, "Failed to authenticate");
                }

                // Call Paymob transaction inquiry API
                var url = $"https://accept.paymob.com/api/acceptance/transactions/{paymobTransactionId}";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");

                var response = await _httpClient.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to verify refund: {responseContent}");
                    return (false, "Verification failed");
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
                if (result == null)
                {
                    return (false, "Invalid response");
                }

                var status = result.ContainsKey("refunded") && result["refunded"].GetBoolean()
                    ? "Refunded"
                    : "Processing";

                _logger.LogInformation($"Refund status: {status}");

                return (true, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying refund status");
                return (false, "Verification error");
            }
        }

        #endregion

        #region Handle Webhook

        public async Task<bool> HandleRefundWebhookAsync(Dictionary<string, string> callbackData)
        {
            try
            {
                _logger.LogInformation("🔔 Processing refund webhook callback");

                // Extract refund status from callback
                if (!callbackData.TryGetValue("is_refunded", out var isRefunded))
                {
                    _logger.LogWarning("⚠️ No refund status in webhook");
                    return false;
                }

                var refundCompleted = isRefunded.ToLower() == "true" || isRefunded == "1";

                _logger.LogInformation($"Refund completed: {refundCompleted}");

                return refundCompleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling refund webhook");
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Get Paymob authentication token
        /// </summary>
        private async Task<string> GetAuthTokenAsync()
        {
            try
            {
                var requestBody = new { api_key = PaymobApiKey };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://accept.paymob.com/api/auth/tokens",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to authenticate with Paymob: {responseContent}");
                    return string.Empty;
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);
                return result?["token"].GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auth token");
                return string.Empty;
            }
        }

        /// <summary>
        /// Process refund via Paymob API
        /// Endpoint: POST https://accept.paymob.com/api/acceptance/void_refund/refund
        /// </summary>
        private async Task<(bool Success, string Message, string? TransactionId)> ProcessPaymobRefundAsync(
            string authToken,
            string transactionId,
            decimal refundAmount,
            string currency)
        {
            try
            {
                // Convert amount to cents (Paymob requires amounts in cents)
                var amountCents = (int)(refundAmount * 100);

                _logger.LogInformation($"Processing refund: Transaction {transactionId}, Amount {amountCents} cents");

                var requestBody = new
                {
                    auth_token = authToken,
                    transaction_id = transactionId,
                    amount_cents = amountCents
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending refund request to Paymob API...");
                _logger.LogInformation($"Request body: {json}");

                var response = await _httpClient.PostAsync(
                    "https://accept.paymob.com/api/acceptance/void_refund/refund",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Paymob response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ Paymob refund API failed: {responseContent}");
                    return (false, $"Payment gateway error: {responseContent}", null);
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                if (result == null)
                {
                    return (false, "Invalid response from payment gateway", null);
                }

                // Extract refund transaction ID from response
                string? refundTransactionId = null;
                if (result.ContainsKey("id"))
                {
                    refundTransactionId = result["id"].ToString();
                }
                else if (result.ContainsKey("transaction_id"))
                {
                    refundTransactionId = result["transaction_id"].ToString();
                }

                _logger.LogInformation($"✅ Paymob refund API success. Refund Transaction ID: {refundTransactionId}");

                return (true, "Refund initiated successfully", refundTransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Paymob refund API");
                return (false, $"Refund API error: {ex.Message}", null);
            }
        }

        #endregion
    }
}