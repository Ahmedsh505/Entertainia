using Core.Models;
using Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.PaymentModels;
using Core.Models.BookingModels;
using Microsoft.Extensions.DependencyInjection;

namespace Service.Services
{
    /// <summary>
    /// ✅ ENHANCED: Merged Payment Service with Complete Payment History Tracking
    /// Logs all payment status transitions automatically
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentService> _logger;
        private readonly IPaymentHistoryService _paymentHistoryService; // ✅ ADDED
        private readonly IServiceProvider _serviceProvider;
        private readonly IEgyptTimeService _timeService;

        // Paymob Configuration Keys
        private string PaymobApiKey => _configuration["Paymob:ApiKey"] ?? "";
        private string PaymobIntegrationId => _configuration["Paymob:IntegrationId"] ?? "";
        private string PaymobIframeId => _configuration["Paymob:IframeId"] ?? "";
        private string PaymobHmacSecret => _configuration["Paymob:HmacSecret"] ?? "";
        private string PaymobCallbackUrl => _configuration["Paymob:CallbackUrl"] ?? "";

        public PaymentService(
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<PaymentService> logger,
            IPaymentHistoryService paymentHistoryService,
            IServiceProvider serviceProvider,
            IEgyptTimeService timeService) // ✅ ADDED
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _paymentHistoryService = paymentHistoryService; // ✅ ADDED
            _serviceProvider = serviceProvider;
            _timeService = timeService;
        }

        #region Payment Initiation

        public async Task<Payment> InitiatePaymentAsync(Booking booking)
        {
            try
            {
                _logger.LogInformation($"Initiating payment for booking {booking.BookingId}");

                // Calculate tax
                booking.CalculateTax(14m);

                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = booking.TotalPrice - booking.DiscountAmount,
                    Currency = booking.Currency,
                    Status = PaymentStatus.Pending,
                    PaymentGateway = "Paymob",
                    TransactionId = GenerateTransactionId(),
                    CreatedAt = DateTime.UtcNow,
                    TaxAmount = booking.TaxAmount,
                    TotalAmountWithTax = booking.TotalWithTax
                };

                _unitOfWork.Payments.Add(payment);
                _unitOfWork.Complete();

                _logger.LogInformation($"Payment initiated: ID {payment.PaymentId}, Transaction {payment.TransactionId}");

                // ✅ NEW: Log payment initiation to history
                await _paymentHistoryService.LogAsync(
                    booking.UserId,
                    booking.BookingId,
                    payment.TotalAmountWithTax,
                    PaymentStatus.Pending,
                    method: "Paymob",
                    transactionId: payment.TransactionId
                );
                _logger.LogInformation($"Payment history logged: Pending status for booking {booking.BookingId}");

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initiating payment for booking {booking.BookingId}");
                throw;
            }
        }

        #endregion

        #region Payment Processing

        public async Task<bool> ProcessPaymentAsync(int paymentId, string transactionId)
        {
            try
            {
                _logger.LogInformation($"Processing payment {paymentId}");

                var payment = _unitOfWork.Payments.GetById(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning($"Payment {paymentId} not found");
                    return false;
                }

                payment.Status = PaymentStatus.Processing;
                payment.TransactionId = transactionId;
                payment.ProcessedAt = DateTime.UtcNow;

                _unitOfWork.Payments.Update(payment);
                _unitOfWork.Complete();

                _logger.LogInformation($"Payment {paymentId} marked as processing");

                // ✅ NEW: Log status change to history
                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking != null)
                {
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        payment.BookingId,
                        payment.TotalAmountWithTax,
                        PaymentStatus.Processing,
                        method: "Paymob",
                        transactionId: transactionId
                    );
                    _logger.LogInformation($"Payment history updated: Processing status for payment {paymentId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing payment {paymentId}");
                return false;
            }
        }

        // ✅ UPDATED: Now generates tickets after payment completion
        public async Task<bool> CompletePaymentAsync(int paymentId)
        {
            try
            {
                _logger.LogInformation($"Completing payment {paymentId}");

                var payment = _unitOfWork.Payments.GetById(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning($"Payment {paymentId} not found");
                    return false;
                }

                payment.Status = PaymentStatus.Completed;
                payment.CompletedAt = DateTime.UtcNow;

                // Update booking status
                if (payment.Booking != null)
                {
                    payment.Booking.Status = "Confirmed";
                    _unitOfWork.Bookings.Update(payment.Booking);
                }

                _unitOfWork.Payments.Update(payment);
                _unitOfWork.Complete();

                _logger.LogInformation($"Payment {paymentId} completed successfully");

                // ✅ Log completion to history
                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking != null)
                {
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        payment.BookingId,
                        payment.TotalAmountWithTax,
                        PaymentStatus.Completed,
                        method: payment.PaymentMethod ?? "Paymob",
                        transactionId: payment.TransactionId
                    );
                    _logger.LogInformation($"Payment history updated: Completed status for payment {paymentId}");

                    // ✅ NEW: Generate tickets after successful payment
                    try
                    {
                        // Get ticket service from service provider
                        // You'll need to inject IServiceProvider into PaymentService
                        var ticketService = _serviceProvider.GetRequiredService<ITicketService>();

                        var ticketResult = await ticketService.GenerateTicketsForBookingAsync(payment.BookingId);

                        if (ticketResult.Success)
                        {
                            _logger.LogInformation($"✅ {ticketResult.Tickets?.Count ?? 0} tickets generated for booking {payment.BookingId}");

                            // ✅ Send tickets via email
                            await ticketService.SendTicketsViaEmailAsync(payment.BookingId);
                            _logger.LogInformation($"✅ Tickets sent via email for booking {payment.BookingId}");
                        }
                        else
                        {
                            _logger.LogError($"❌ Failed to generate tickets: {ticketResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error generating tickets after payment completion");
                        // Don't fail the payment if ticket generation fails
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error completing payment {paymentId}");
                return false;
            }
        }

        public async Task<bool> FailPaymentAsync(int paymentId, string reason)
        {
            try
            {
                _logger.LogInformation($"Failing payment {paymentId}: {reason}");

                var payment = _unitOfWork.Payments.GetById(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning($"Payment {paymentId} not found");
                    return false;
                }

                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.FailureReason = reason;

                // Update booking status
                if (payment.Booking != null)
                {
                    payment.Booking.Status = "Cancelled";
                    _unitOfWork.Bookings.Update(payment.Booking);
                }

                _unitOfWork.Payments.Update(payment);
                _unitOfWork.Complete();

                _logger.LogInformation($"Payment {paymentId} marked as failed");

                // ✅ NEW: Log failure to history
                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking != null)
                {
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        payment.BookingId,
                        payment.TotalAmountWithTax,
                        PaymentStatus.Failed,
                        method: payment.PaymentMethod ?? "Paymob",
                        transactionId: payment.TransactionId
                    );
                    _logger.LogInformation($"Payment history updated: Failed status for payment {paymentId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error failing payment {paymentId}");
                return false;
            }
        }

        /// <summary>
        /// ✅ NEW: Mark payment as refunded and log to history
        /// </summary>
        public async Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount)
        {
            try
            {
                _logger.LogInformation($"Refunding payment {paymentId}: Amount {refundAmount}");

                var payment = _unitOfWork.Payments.GetById(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning($"Payment {paymentId} not found");
                    return false;
                }

                payment.Status = PaymentStatus.Refunded;
                _unitOfWork.Payments.Update(payment);
                _unitOfWork.Complete();

                _logger.LogInformation($"Payment {paymentId} marked as refunded");

                // ✅ Log refund to history
                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking != null)
                {
                    await _paymentHistoryService.LogAsync(
                        booking.UserId,
                        payment.BookingId,
                        refundAmount, // Log the refund amount (80% of original)
                        PaymentStatus.Refunded,
                        method: payment.PaymentMethod ?? "Paymob Refund",
                        transactionId: payment.TransactionId
                    );
                    _logger.LogInformation($"Payment history updated: Refunded status for payment {paymentId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refunding payment {paymentId}");
                return false;
            }
        }

        #endregion

        #region Payment Retrieval

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await Task.Run(() => _unitOfWork.Payments.GetById(paymentId));
        }

        public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
        {
            return await Task.Run(() => _unitOfWork.Payments.GetByTransactionId(transactionId));
        }

        #endregion

        #region Paymob Integration

        public async Task<string> GeneratePaymentUrlAsync(Payment payment)
        {
            try
            {
                _logger.LogInformation("=== Starting Payment URL Generation ===");
                _logger.LogInformation($"Payment ID: {payment.PaymentId}");
                _logger.LogInformation($"Booking ID: {payment.BookingId}");
                _logger.LogInformation($"Transaction ID: {payment.TransactionId}");
                _logger.LogInformation($"Amount: {payment.TotalAmountWithTax} {payment.Currency}");

                // Get booking
                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);

                if (booking == null)
                {
                    _logger.LogError($"❌ Booking {payment.BookingId} not found!");
                    throw new Exception("Booking not found");
                }

                _logger.LogInformation($"Booking found: {booking.BookingId} for event '{booking.Event?.Title}'");

                // Get user
                var user = booking.User;

                if (user == null)
                {
                    _logger.LogError($"❌ User not found for booking {booking.BookingId}!");
                    throw new Exception("User not found");
                }

                _logger.LogInformation($"User found: {user.Email}");

                // Validate Paymob configuration
                if (string.IsNullOrEmpty(PaymobApiKey))
                {
                    _logger.LogError("❌ Paymob API Key is not configured!");
                    throw new Exception("Paymob API Key is missing");
                }

                if (string.IsNullOrEmpty(PaymobIntegrationId))
                {
                    _logger.LogError("❌ Paymob Integration ID is not configured!");
                    throw new Exception("Paymob Integration ID is missing");
                }

                if (string.IsNullOrEmpty(PaymobIframeId))
                {
                    _logger.LogError("❌ Paymob Iframe ID is not configured!");
                    throw new Exception("Paymob Iframe ID is missing");
                }

                _logger.LogInformation("✓ Paymob configuration validated");

                // STEP 1: AUTH TOKEN
                _logger.LogInformation("Step 1: Requesting auth token from Paymob...");
                var authToken = await GetAuthTokenAsync();
                _logger.LogInformation($"✓ Auth token received (length: {authToken?.Length ?? 0})");

                // STEP 2: ORDER
                _logger.LogInformation("Step 2: Registering order with Paymob...");
                var orderId = await RegisterOrderAsync(authToken, payment, booking);
                _logger.LogInformation($"✓ Order registered with ID: {orderId}");

                // STEP 3: PAYMENT KEY
                _logger.LogInformation("Step 3: Generating payment key...");
                var paymentToken = await GeneratePaymentKeyAsync(authToken, orderId, payment, booking, user);
                _logger.LogInformation($"✓ Payment token generated (length: {paymentToken?.Length ?? 0})");

                // Final URL
                var baseUrl = _configuration["Paymob:PaymentUrl"]
                              ?? "https://accept.paymob.com/api/acceptance/iframes";

                var paymentUrl = $"{baseUrl}/{PaymobIframeId}?payment_token={paymentToken}";

                _logger.LogInformation("✓ Payment URL generated successfully");
                _logger.LogInformation("=== Payment URL Generation Complete ===");

                return paymentUrl;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "❌ HTTP request failed when connecting to Paymob");
                throw new Exception($"Failed to connect to Paymob payment gateway: {httpEx.Message}", httpEx);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "❌ Failed to parse Paymob response");
                throw new Exception($"Invalid response from Paymob: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Payment URL generation failed");
                throw new Exception($"Payment initialization failed: {ex.Message}", ex);
            }
        }


        public async Task<bool> VerifyPaymentCallbackAsync(Dictionary<string, string> callbackData)
        {
            try
            {
                if (string.IsNullOrEmpty(PaymobHmacSecret))
                {
                    _logger.LogWarning("HMAC secret not configured. Skipping signature verification.");
                    return true;
                }

                if (!callbackData.TryGetValue("hmac", out var receivedHmac))
                {
                    _logger.LogWarning("No HMAC signature in callback");
                    return false;
                }

                var concatenatedString = string.Join("", new[]
                {
                    callbackData.GetValueOrDefault("amount_cents", ""),
                    callbackData.GetValueOrDefault("created_at", ""),
                    callbackData.GetValueOrDefault("currency", ""),
                    callbackData.GetValueOrDefault("error_occured", ""),
                    callbackData.GetValueOrDefault("has_parent_transaction", ""),
                    callbackData.GetValueOrDefault("id", ""),
                    callbackData.GetValueOrDefault("integration_id", ""),
                    callbackData.GetValueOrDefault("is_3d_secure", ""),
                    callbackData.GetValueOrDefault("is_auth", ""),
                    callbackData.GetValueOrDefault("is_capture", ""),
                    callbackData.GetValueOrDefault("is_refunded", ""),
                    callbackData.GetValueOrDefault("is_standalone_payment", ""),
                    callbackData.GetValueOrDefault("is_voided", ""),
                    callbackData.GetValueOrDefault("order", ""),
                    callbackData.GetValueOrDefault("owner", ""),
                    callbackData.GetValueOrDefault("pending", ""),
                    callbackData.GetValueOrDefault("source_data_pan", ""),
                    callbackData.GetValueOrDefault("source_data_sub_type", ""),
                    callbackData.GetValueOrDefault("source_data_type", ""),
                    callbackData.GetValueOrDefault("success", "")
                });

                var calculatedHmac = CalculateHmac(concatenatedString, PaymobHmacSecret);
                var isValid = calculatedHmac.Equals(receivedHmac, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning($"HMAC validation failed. Expected: {calculatedHmac}, Received: {receivedHmac}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment callback");
                return false;
            }
        }

        public async Task<bool> VerifyPaymentCallbackAsync(string transactionId, string signature)
        {
            return true; // Backward compatibility
        }

        #endregion

        #region Organizer Dashboard Helpers

        public async Task<decimal> GetTotalRevenueForOrganizerAsync(int organizerId)
        {
            return await Task.Run(() => _unitOfWork.Payments.GetTotalRevenueForOrganizer(organizerId));
        }

        public async Task<decimal> GetMonthlyRevenueForOrganizerAsync(int organizerId, int year, int month)
        {
            return await Task.Run(() => _unitOfWork.Payments.GetMonthlyRevenueForOrganizer(organizerId, year, month));
        }

        public async Task<int> GetTotalTicketsSoldForOrganizerAsync(int organizerId)
        {
            return await Task.Run(() =>
            {
                var bookings = _unitOfWork.Bookings.GetAll()
                    .Where(b => b.Event.OrganizerId == organizerId && b.Status == "Confirmed")
                    .ToList();

                return bookings.Sum(b => b.NumberOfTickets);
            });
        }

        public async Task<int> GetMonthlyTicketsSoldForOrganizerAsync(int organizerId, int year, int month)
        {
            return await Task.Run(() =>
            {
                var bookings = _unitOfWork.Bookings.GetAll()
                    .Where(b => b.Event.OrganizerId == organizerId &&
                                b.Status == "Confirmed" &&
                                b.BookingDate.Year == year &&
                                b.BookingDate.Month == month)
                    .ToList();

                return bookings.Sum(b => b.NumberOfTickets);
            });
        }

        public async Task<(decimal TotalRevenue, decimal TotalTax, decimal TotalDiscount)> GetOrganizerFinancialsAsync(int organizerId)
        {
            return await Task.Run(() =>
            {
                var bookings = _unitOfWork.Bookings.GetAll()
                    .Where(b => b.Event.OrganizerId == organizerId && b.Status == "Confirmed")
                    .ToList();

                var totalRevenue = bookings.Sum(b => b.TotalWithTax);
                var totalTax = bookings.Sum(b => b.TaxAmount);
                var totalDiscount = bookings.Sum(b => b.DiscountAmount);

                return (totalRevenue, totalTax, totalDiscount);
            });
        }

        #endregion

        #region Private Helper Methods

        private string GenerateTransactionId()
        {
            return $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

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
                    throw new Exception($"Failed to authenticate with Paymob: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                if (result == null || !result.ContainsKey("token"))
                {
                    throw new Exception("No auth token received from Paymob");
                }

                return result["token"].GetString() ?? throw new Exception("Auth token is null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAuthTokenAsync failed");
                throw;
            }
        }

        private async Task<string> RegisterOrderAsync(string authToken, Payment payment, Booking booking)
        {
            try
            {
                var amountCents = (int)(payment.TotalAmountWithTax * 100);

                var requestBody = new
                {
                    auth_token = authToken,
                    delivery_needed = "false",
                    amount_cents = amountCents,
                    currency = payment.Currency.ToString(),
                    merchant_order_id = payment.TransactionId,
                    items = new[]
                    {
                        new
                        {
                            name = booking.Event?.Title ?? "Event Ticket",
                            amount_cents = amountCents,
                            description = $"{booking.NumberOfTickets}x {booking.TicketTier} tickets",
                            quantity = booking.NumberOfTickets
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://accept.paymob.com/api/ecommerce/orders",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to register order with Paymob: {responseContent}");
                    throw new Exception($"Failed to register order with Paymob: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                if (result == null || !result.ContainsKey("id"))
                {
                    throw new Exception("No order ID received from Paymob");
                }

                return result["id"].GetInt32().ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RegisterOrderAsync failed");
                throw;
            }
        }

        private async Task<string> GeneratePaymentKeyAsync(
            string authToken,
            string orderId,
            Payment payment,
            Booking booking,
            User user)
        {
            try
            {
                var amountCents = (int)(payment.TotalAmountWithTax * 100);

                var requestBody = new
                {
                    auth_token = authToken,
                    amount_cents = amountCents,
                    expiration = 3600,
                    order_id = orderId,
                    billing_data = new
                    {
                        apartment = "NA",
                        email = user.Email ?? "customer@example.com",
                        floor = "NA",
                        first_name = user.FirstName ?? "Customer",
                        street = "NA",
                        building = "NA",
                        phone_number = user.PhoneNumber ?? "+20100000000",
                        shipping_method = "NA",
                        postal_code = "NA",
                        city = "Cairo",
                        country = "EG",
                        last_name = user.LastName ?? "User",
                        state = "NA"
                    },
                    currency = payment.Currency.ToString(),
                    integration_id = int.Parse(PaymobIntegrationId),
                    lock_order_when_paid = "true"
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    "https://accept.paymob.com/api/acceptance/payment_keys",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to generate payment key: {responseContent}");
                    throw new Exception($"Failed to generate payment key: {responseContent}");
                }

                var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                if (result == null || !result.ContainsKey("token"))
                {
                    throw new Exception("No payment token received from Paymob");
                }

                return result["token"].GetString() ?? throw new Exception("Payment token is null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GeneratePaymentKeyAsync failed");
                throw;
            }
        }

        private string CalculateHmac(string data, string secret)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        #endregion
    }
}