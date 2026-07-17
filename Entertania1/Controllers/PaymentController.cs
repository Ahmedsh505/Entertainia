using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Services;
using System.Text.Json;

namespace Entertania1.Controllers
{
    /// <summary>
    /// ✅ UPDATED: Unified webhook handling for both payments and refunds
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBookingService _bookingService;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentController> _logger;
        private readonly ITransactionService _transactionService;
        private readonly ITicketService _ticketService; 


        public PaymentController(
            IPaymentService paymentService,
            IBookingService bookingService,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<PaymentController> logger,
            ITransactionService transactionService,
            ITicketService ticketService)
        {
            _paymentService = paymentService;
            _bookingService = bookingService;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _transactionService = transactionService;
            _ticketService = ticketService;
        }

        /// <summary>
        /// ✅ UNIFIED WEBHOOK - Handles BOTH payment and refund notifications from Paymob
        /// Configure in Paymob Dashboard → Transaction Processed Callback
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymobWebhook([FromBody] JsonElement webhookData)
        {
            try
            {
                _logger.LogInformation("🔔 Paymob webhook received");
                _logger.LogInformation($"📦 Webhook payload: {webhookData.GetRawText()}");

                // Extract transaction data
                if (!webhookData.TryGetProperty("obj", out var obj))
                {
                    _logger.LogWarning("⚠️ Webhook missing 'obj' property");
                    return BadRequest("Invalid webhook structure");
                }

                // ===== STEP 1: DETERMINE TRANSACTION TYPE =====
                bool isRefunded = obj.TryGetProperty("is_refunded", out var isRefundedProp) && isRefundedProp.GetBoolean();
                bool hasRefundedAmount = obj.TryGetProperty("refunded_amount_cents", out var refundedAmountProp) && refundedAmountProp.GetInt32() > 0;

                // Check if this is a refund transaction
                bool isRefundTransaction = isRefunded || hasRefundedAmount;

                if (isRefundTransaction)
                {
                    // ===== HANDLE REFUND WEBHOOK =====
                    _logger.LogInformation("🔄 Processing REFUND webhook...");
                    return await HandleRefundWebhook(obj);
                }
                else
                {
                    // ===== HANDLE PAYMENT WEBHOOK =====
                    _logger.LogInformation("💳 Processing PAYMENT webhook...");
                    return await HandlePaymentWebhook(obj);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing Paymob webhook");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// ✅ HELPER METHOD: Handle Payment Webhooks
        /// </summary>
        private async Task<IActionResult> HandlePaymentWebhook(JsonElement obj)
        {
            try
            {
                var transactionId = obj.GetProperty("id").GetInt32().ToString();
                var success = obj.GetProperty("success").GetBoolean();
                var pending = obj.GetProperty("pending").GetBoolean();
                var errorOccurred = obj.GetProperty("error_occured").GetBoolean();

                // Get merchant order ID
                var order = obj.GetProperty("order");
                var merchantOrderId = order.GetProperty("merchant_order_id").GetString();

                if (string.IsNullOrEmpty(merchantOrderId))
                {
                    _logger.LogWarning("⚠️ Webhook missing merchant_order_id");
                    return BadRequest("Invalid webhook data: missing merchant_order_id");
                }

                _logger.LogInformation($"📋 Processing payment webhook for order: {merchantOrderId}");
                _logger.LogInformation($"✅ Success: {success} | ⏳ Pending: {pending} | ❌ Error: {errorOccurred}");

                // Find payment by merchant order ID
                var payment = _unitOfWork.Payments
                    .GetAll()
                    .FirstOrDefault(p => p.TransactionId == merchantOrderId);

                if (payment == null)
                {
                    _logger.LogWarning($"⚠️ Payment not found for order: {merchantOrderId}");
                    return NotFound($"Payment not found: {merchantOrderId}");
                }

                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking == null)
                {
                    _logger.LogWarning($"⚠️ Booking not found for payment: {payment.PaymentId}");
                    return NotFound("Booking not found");
                }

                // Update payment with Paymob transaction ID
                payment.PaymentGatewayReference = transactionId;

                // ✅ HANDLE PAYMENT STATUS
                if (success && !errorOccurred && !pending)
                {
                    // ✅ PAYMENT SUCCESSFUL
                    _logger.LogInformation($"✅ Payment SUCCESSFUL for order: {merchantOrderId}");

                    await _paymentService.CompletePaymentAsync(payment.PaymentId);

                    booking.Status = "Confirmed";
                    _unitOfWork.Bookings.Update(booking);
                    _unitOfWork.Complete();

                    // ✅ Log successful payment transaction
                    await _transactionService.LogPaymentAsync(
                        userId: booking.UserId,
                        bookingId: booking.BookingId,
                        paymentId: payment.PaymentId,
                        amount: payment.TotalAmountWithTax,
                        currency: payment.Currency,
                        status: PaymentStatus.Completed,
                        paymentMethod: "Paymob",
                        transactionReference: merchantOrderId,
                        gatewayTransactionId: transactionId
                    );

                    // ✅ GENERATE TICKETS AFTER PAYMENT CONFIRMATION
                    _logger.LogInformation($"💳 Payment confirmed for booking {booking.BookingId}, generating tickets...");

                    var ticketResult = await _ticketService.GenerateTicketsForBookingAsync(booking.BookingId);

                    if (ticketResult.Success && ticketResult.Tickets != null)
                    {
                        _logger.LogInformation($"✅ Generated {ticketResult.Tickets.Count} tickets");

                        // Send tickets via email with PDF
                        var emailSent = await _ticketService.SendTicketsViaEmailAsync(booking.BookingId);
                        _logger.LogInformation($"📧 Email sent status: {emailSent}");
                    }
                    else
                    {
                        _logger.LogError($"❌ Failed to generate tickets: {ticketResult.Message}");
                    }

                    // Send confirmation email (kept for backward compatibility)
                    try
                    {
                        var user = booking.User;
                        if (user != null && booking.Event != null)
                        {
                            await _emailService.SendPaymentConfirmationAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                booking.Event.Title,
                                payment.TotalAmountWithTax,
                                payment.Currency.ToString()
                            );
                            _logger.LogInformation($"📧 Confirmation email sent to {user.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to send payment confirmation email");
                    }

                    return Ok(new { message = "Payment completed successfully", status = "success" });
                }
                else if (pending)
                {
                    // ⏳ PAYMENT PENDING
                    _logger.LogInformation($"⏳ Payment PENDING for order: {merchantOrderId}");

                    payment.Status = PaymentStatus.Processing;
                    _unitOfWork.Payments.Update(payment);
                    _unitOfWork.Complete();

                    return Ok(new { message = "Payment is pending", status = "pending" });
                }
                else
                {
                    // ❌ PAYMENT FAILED - IMMEDIATELY CANCEL
                    _logger.LogInformation($"❌ Payment FAILED for order: {merchantOrderId}");

                    var failReason = errorOccurred ? "Payment error occurred" : "Payment declined";
                    await _paymentService.FailPaymentAsync(payment.PaymentId, failReason);

                    // ✅ IMMEDIATELY CANCEL BOOKING AND RESTORE SEATS
                    booking.Status = "Cancelled";
                    _unitOfWork.Bookings.Update(booking);

                    await _bookingService.RestoreTierSeatsAsync(
                        booking.EventId,
                        booking.TicketTier,
                        booking.NumberOfTickets
                    );

                    // Restore discount code usage if applicable
                    if (booking.DiscountCodeId.HasValue)
                    {
                        var discount = _unitOfWork.DiscountCodes.GetById(booking.DiscountCodeId.Value);
                        if (discount != null && discount.CurrentUsageCount > 0)
                        {
                            discount.CurrentUsageCount--;
                            _unitOfWork.DiscountCodes.Update(discount);
                        }
                    }

                    _unitOfWork.Complete();

                    // Send failure email
                    try
                    {
                        var user = booking.User;
                        if (user != null && booking.Event != null)
                        {
                            await _emailService.SendEmailFailureAsync(
                                user.Email,
                                "Payment Failed - Booking Cancelled",
                                $"Dear {user.FirstName},<br><br>" +
                                $"Your payment for '{booking.Event.Title}' could not be processed.<br>" +
                                $"Reason: {failReason}<br><br>" +
                                $"Your booking has been automatically cancelled and seats have been released.<br>" +
                                $"You can try booking again from the event page.<br><br>" +
                                $"Thank you for your understanding."
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to send failure email");
                    }

                    return Ok(new { message = "Payment failed, booking cancelled immediately", status = "failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in HandlePaymentWebhook");
                return StatusCode(500, new { message = "Payment webhook processing failed", error = ex.Message });
            }
        }

        /// <summary>
        /// ✅ HELPER METHOD: Handle Refund Webhooks
        /// </summary>
        private async Task<IActionResult> HandleRefundWebhook(JsonElement obj)
        {
            try
            {
                _logger.LogInformation("🔄 Processing refund webhook...");

                var refundTransactionId = obj.GetProperty("id").GetInt32().ToString();
                var isRefunded = obj.TryGetProperty("is_refunded", out var isRefundedProp) && isRefundedProp.GetBoolean();
                var success = obj.GetProperty("success").GetBoolean();

                _logger.LogInformation($"Refund Transaction ID: {refundTransactionId}");
                _logger.LogInformation($"Is Refunded: {isRefunded}, Success: {success}");

                // Find refund by Paymob transaction ID
                var refund = _unitOfWork.Refunds.GetAll()
                    .FirstOrDefault(r => r.RefundTransactionId == refundTransactionId
                                      || r.PaymentGatewayReference == refundTransactionId);

                if (refund == null)
                {
                    _logger.LogWarning($"⚠️ Refund not found for Paymob transaction: {refundTransactionId}");
                    return NotFound($"Refund not found: {refundTransactionId}");
                }

                _logger.LogInformation($"Found refund: ID {refund.RefundId}, Current Status: {refund.Status}");

                // Only update if refund is in Processing state
                if (refund.Status != PaymentStatus.Processing && refund.Status != PaymentStatus.Refunded)
                {
                    _logger.LogWarning($"⚠️ Refund {refund.RefundId} is not in Processing state (current: {refund.Status})");
                    return Ok(new { message = "Refund status already final", status = refund.Status });
                }

                if (isRefunded && success)
                {
                    // ✅ REFUND COMPLETED
                    _logger.LogInformation($"✅ Refund COMPLETED for refund ID: {refund.RefundId}");

                    refund.Status = PaymentStatus.Refunded;
                    refund.CompletedAt = DateTime.UtcNow;

                    // Update booking
                    if (refund.Booking != null)
                    {
                        refund.Booking.Status = "Refunded";
                        _unitOfWork.Bookings.Update(refund.Booking);
                    }

                    // Update payment
                    if (refund.Payment != null)
                    {
                        refund.Payment.Status = PaymentStatus.Refunded;
                        _unitOfWork.Payments.Update(refund.Payment);
                    }

                    _unitOfWork.Refunds.Update(refund);
                    _unitOfWork.Complete();

                    // ✅ Log refund completion transaction
                    await _transactionService.LogRefundAsync(
                        userId: refund.RequestedByUserId,
                        bookingId: refund.BookingId,
                        refundId: refund.RefundId,
                        amount: refund.RefundAmount,
                        currency: refund.Currency,
                        status: PaymentStatus.Refunded,
                        transactionReference: $"REFUND-WEBHOOK-{refund.RefundId}",
                        gatewayTransactionId: refundTransactionId
                    );

                    // Send completion email
                    try
                    {
                        var user = refund.RequestedBy;
                        if (user != null && refund.Booking?.Event != null)
                        {
                            await _emailService.SendRefundCompletedAsync(
                                user.Email,
                                $"{user.FirstName} {user.LastName}",
                                refund.Booking.Event.Title ?? "Event",
                                refund.RefundAmount,
                                refund.Currency.ToString()
                            );
                            _logger.LogInformation($"📧 Refund completion email sent to {user.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to send refund completion email");
                    }

                    return Ok(new { message = "Refund completed successfully", status = "refunded" });
                }
                else
                {
                    // ❌ REFUND FAILED
                    _logger.LogInformation($"❌ Refund FAILED for refund ID: {refund.RefundId}");

                    refund.Status = PaymentStatus.Failed;
                    refund.FailedAt = DateTime.UtcNow;
                    refund.FailureReason = "Refund processing failed at payment gateway";

                    // Update booking status back to Confirmed (since refund failed)
                    if (refund.Booking != null)
                    {
                        refund.Booking.Status = "Refund Failed";
                        _unitOfWork.Bookings.Update(refund.Booking);
                    }

                    _unitOfWork.Refunds.Update(refund);
                    _unitOfWork.Complete();

                    return Ok(new { message = "Refund failed", status = "failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in HandleRefundWebhook");
                return StatusCode(500, new { message = "Refund webhook processing failed", error = ex.Message });
            }
        }

        /// <summary>
        /// ✅ PAYMOB CALLBACK - User redirect after payment
        /// Configure in Paymob Dashboard → Transaction Response Callback
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymobCallback(
            [FromQuery] string? success,
            [FromQuery] string? id,
            [FromQuery] string? order,
            [FromQuery] string? pending,
            [FromQuery] string? amount_cents,
            [FromQuery] string? currency,
            [FromQuery] string? error_occured,
            [FromQuery] string? merchant_order_id,
            [FromQuery] string? hmac)
        {
            try
            {
                var merchantOrderId = order ?? merchant_order_id;

                _logger.LogInformation($"🔙 Payment callback received for order: {merchantOrderId}");

                if (string.IsNullOrEmpty(merchantOrderId))
                {
                    return RedirectToAction("MyBookings", "Booking", new { error = "Invalid payment response" });
                }

                // Find payment
                var payment = _unitOfWork.Payments
                    .GetAll()
                    .FirstOrDefault(p => p.TransactionId == merchantOrderId);

                if (payment == null)
                {
                    return RedirectToAction("MyBookings", "Booking", new { error = "Payment not found" });
                }

                var booking = _unitOfWork.Bookings.GetById(payment.BookingId);
                if (booking == null)
                {
                    return RedirectToAction("MyBookings", "Booking", new { error = "Booking not found" });
                }

                // Redirect based on payment status
                var isSuccess = success?.ToLower() == "true";
                var isPending = pending?.ToLower() == "true" || pending == "1";
                var hasError = error_occured?.ToLower() == "true";

                if (isSuccess && !hasError)
                {
                    return RedirectToAction("Details", "Booking", new
                    {
                        bookingId = booking.BookingId,
                        success = "Payment completed successfully! Your booking is confirmed."
                    });
                }
                else if (isPending)
                {
                    return RedirectToAction("MyBookings", "Booking", new
                    {
                        info = "Payment is being processed. Please wait a moment..."
                    });
                }
                else
                {
                    // ❌ Payment failed - booking already cancelled by webhook
                    return RedirectToAction("MyBookings", "Booking", new
                    {
                        error = "Payment failed. Your booking has been cancelled. You can try booking again."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing payment callback");
                return RedirectToAction("MyBookings", "Booking", new { error = "An error occurred" });
            }
        }

        /// <summary>
        /// ✅ Test endpoint
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Payment webhook is accessible",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            });
        }
    }
}