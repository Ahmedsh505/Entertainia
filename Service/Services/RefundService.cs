using Core.Enums;
using Core.Interfaces.ServicesInterface;
using Core.Interfaces.UnitOfWorkInterface;
using Core.Models.PaymentModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Service.Services
{
    /// <summary>
    /// ✅ COMPLETE: Production-ready refund service with Paymob integration
    /// </summary>
    public class RefundService : IRefundService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<RefundService> _logger;
        private readonly IPaymobRefundService _paymobRefundService;
        private readonly ITransactionService _transactionService;
        private readonly IConfiguration _configuration;
        private readonly IEgyptTimeService _timeService;

        // ✅ DYNAMIC: Configurable refund percentage
        private decimal DefaultRefundPercentage =>
            decimal.TryParse(_configuration["Refund:DefaultPercentage"], out var value) ? value : 80m;

        public RefundService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<RefundService> logger,
            IPaymobRefundService paymobRefundService,
            ITransactionService transactionService,
            IConfiguration configuration,
            IEgyptTimeService timeService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
            _paymobRefundService = paymobRefundService;
            _transactionService = transactionService;
            _configuration = configuration;
            _timeService = timeService;
        }

        #region Request Refund

        public async Task<(bool Success, string Message, Refund? Refund)> RequestRefundAsync(
            int bookingId,
            int userId,
            string reason)
        {
            try
            {
                _logger.LogInformation($"User {userId} requesting refund for booking {bookingId}");

                var booking = _unitOfWork.Bookings.GetById(bookingId);
                if (booking == null)
                {
                    return (false, "Booking not found", null);
                }

                if (booking.UserId != userId)
                {
                    return (false, "You don't have permission to request a refund for this booking", null);
                }

                if (booking.Status != "Confirmed")
                {
                    return (false, "Only confirmed bookings can be refunded", null);
                }

                var payment = _unitOfWork.Payments.GetByBookingId(bookingId);
                if (payment == null)
                {
                    return (false, "No payment found for this booking", null);
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    return (false, "Payment must be completed before requesting a refund", null);
                }

                var existingRefund = _unitOfWork.Refunds.GetByBookingId(bookingId);
                if (existingRefund != null)
                {
                    return (false, "A refund request already exists for this booking", null);
                }

                // Create refund entity
                var refund = new Refund
                {
                    PaymentId = payment.PaymentId,
                    BookingId = bookingId,
                    OriginalAmount = payment.TotalAmountWithTax,
                    RefundPercentage = DefaultRefundPercentage,
                    Currency = payment.Currency,
                    Status = PaymentStatus.Pending,
                    RequestedByUserId = userId,
                    Reason = reason,
                    RequestedAt = DateTime.UtcNow
                };

                refund.CalculateRefund();

                _unitOfWork.Refunds.Add(refund);

                // Update booking status to "Pending Refund"
                booking.Status = "Pending Refund";
                _unitOfWork.Bookings.Update(booking);

                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Refund request created: ID {refund.RefundId}");

                // ✅ Log transaction
                await _transactionService.LogRefundAsync(
                    userId,
                    bookingId,
                    refund.RefundId,
                    refund.RefundAmount,
                    refund.Currency,
                    PaymentStatus.Pending,
                    transactionReference: $"REFUND-REQ-{refund.RefundId}"
                );

                return (true, $"Refund request submitted successfully. You will receive {refund.RefundAmount:N2} {refund.Currency} ({refund.RefundPercentage}% of {refund.OriginalAmount:N2}) once approved.", refund);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error requesting refund for booking {bookingId}");
                return (false, $"An error occurred: {ex.Message}", null);
            }
        }

        #endregion

        #region Approve Refund

        public async Task<(bool Success, string Message)> ApproveRefundAsync(
            int refundId,
            int adminUserId,
            string? note = null)
        {
            try
            {
                _logger.LogInformation($"Admin {adminUserId} approving refund {refundId}");

                var refund = _unitOfWork.Refunds.GetById(refundId);
                if (refund == null)
                {
                    _logger.LogError($"❌ Refund {refundId} not found");
                    return (false, "Refund not found");
                }

                if (refund.Status != PaymentStatus.Pending)
                {
                    _logger.LogWarning($"⚠️ Refund {refundId} is not pending (status: {refund.Status})");
                    return (false, "Only pending refunds can be approved");
                }

                var payment = _unitOfWork.Payments.GetById(refund.PaymentId);
                if (payment == null)
                {
                    _logger.LogError($"❌ Payment {refund.PaymentId} not found");
                    return (false, "Original payment not found");
                }

                // ✅ STEP 1: Update refund to Processing
                refund.Status = PaymentStatus.Processing;
                refund.ProcessedByUserId = adminUserId;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.AdminNotes = note;

                _unitOfWork.Refunds.Update(refund);
                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Refund {refundId} status updated to Processing");

                // ✅ Update transaction log
                await _transactionService.LogRefundAsync(
                    refund.RequestedByUserId,
                    refund.BookingId,
                    refund.RefundId,
                    refund.RefundAmount,
                    refund.Currency,
                    PaymentStatus.Processing,
                    transactionReference: $"REFUND-PROC-{refund.RefundId}"
                );

                // ✅ STEP 2: Process Paymob Refund
                _logger.LogInformation($"🔄 Processing Paymob refund for refund {refundId}");

                var paymobResult = await _paymobRefundService.ProcessRefundAsync(refund, payment);

                if (!paymobResult.Success)
                {
                    // ❌ Paymob refund failed - Update status back to Pending
                    _logger.LogError($"❌ Paymob refund failed: {paymobResult.Message}");

                    refund.Status = PaymentStatus.Failed;
                    refund.FailedAt = DateTime.UtcNow;
                    refund.FailureReason = paymobResult.Message;

                    _unitOfWork.Refunds.Update(refund);
                    _unitOfWork.Complete();

                    // Log failed transaction
                    await _transactionService.LogRefundAsync(
                        refund.RequestedByUserId,
                        refund.BookingId,
                        refund.RefundId,
                        0,
                        refund.Currency,
                        PaymentStatus.Failed,
                        transactionReference: $"REFUND-FAIL-{refund.RefundId}"
                    );

                    return (false, $"Refund processing failed: {paymobResult.Message}");
                }

                // ✅ STEP 3: Paymob refund successful - Update entities
                _logger.LogInformation($"✅ Paymob refund successful. Transaction ID: {paymobResult.PaymobTransactionId}");

                // Store Paymob refund transaction ID
                refund.RefundTransactionId = paymobResult.PaymobTransactionId;
                refund.PaymentGatewayReference = paymobResult.PaymobTransactionId;

                // Mark as completed
                refund.Status = PaymentStatus.Refunded;
                refund.CompletedAt = DateTime.UtcNow;

                _unitOfWork.Refunds.Update(refund);

                // Update booking status
                if (refund.Booking != null)
                {
                    refund.Booking.Status = "Refunded";
                    _unitOfWork.Bookings.Update(refund.Booking);
                }

                // Update payment status
                payment.Status = PaymentStatus.Refunded;
                _unitOfWork.Payments.Update(payment);

                _unitOfWork.Complete();

                _logger.LogInformation($"✅ All entities updated successfully");

                // ✅ Log successful refund transaction
                await _transactionService.LogRefundAsync(
                    refund.RequestedByUserId,
                    refund.BookingId,
                    refund.RefundId,
                    refund.RefundAmount,
                    refund.Currency,
                    PaymentStatus.Refunded,
                    transactionReference: $"REFUND-COMP-{refund.RefundId}",
                    gatewayTransactionId: paymobResult.PaymobTransactionId
                );

                // ✅ Send approval email
                try
                {
                    var user = refund.RequestedBy;
                    if (user != null && refund.Booking?.Event != null)
                    {
                        await _emailService.SendRefundApprovedAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}",
                            refund.Booking.Event.Title ?? "Event",
                            refund.RefundAmount,
                            refund.Currency.ToString()
                        );
                        _logger.LogInformation($"✅ Refund approval email sent to {user.Email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send refund approval email");
                }

                return (true, $"Refund approved and processed successfully. {refund.RefundAmount:N2} {refund.Currency} will be returned to the customer's account in 1-14 business days.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error approving refund {refundId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Reject Refund

        public async Task<(bool Success, string Message)> RejectRefundAsync(
    int refundId,
    int adminUserId,
    string reason)
        {
            try
            {
                _logger.LogInformation($"Admin {adminUserId} rejecting refund {refundId}");

                var refund = _unitOfWork.Refunds.GetById(refundId);
                if (refund == null)
                {
                    _logger.LogError($"❌ Refund {refundId} not found");
                    return (false, "Refund not found");
                }

                if (refund.Status != PaymentStatus.Pending)
                {
                    _logger.LogWarning($"⚠️ Refund {refundId} is not pending (status: {refund.Status})");
                    return (false, "Only pending refunds can be rejected");
                }

                // ✅ CHANGED: Use RefundRejected instead of Failed
                refund.Status = PaymentStatus.RefundRejected;
                refund.ProcessedByUserId = adminUserId;
                refund.ProcessedAt = DateTime.UtcNow;
                refund.FailedAt = DateTime.UtcNow;
                refund.FailureReason = reason;
                refund.AdminNotes = reason;

                _unitOfWork.Refunds.Update(refund);

                // Update booking status
                if (refund.Booking != null)
                {
                    refund.Booking.Status = "Refund Rejected";
                    _unitOfWork.Bookings.Update(refund.Booking);
                }

                _unitOfWork.Complete();

                _logger.LogInformation($"✅ Refund {refundId} rejected");

                // ✅ Log rejection transaction with RefundRejected status
                await _transactionService.LogRefundAsync(
                    refund.RequestedByUserId,
                    refund.BookingId,
                    refund.RefundId,
                    0, // $0 for rejected refunds
                    refund.Currency,
                    PaymentStatus.RefundRejected,
                    transactionReference: $"REFUND-REJ-{refund.RefundId}"
                );

                // Send rejection email
                try
                {
                    var user = refund.RequestedBy;
                    if (user != null && refund.Booking?.Event != null)
                    {
                        await _emailService.SendRefundRejectedAsync(
                            user.Email,
                            $"{user.FirstName} {user.LastName}",
                            refund.Booking.Event.Title ?? "Event",
                            reason
                        );
                        _logger.LogInformation($"✅ Refund rejection email sent to {user.Email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to send refund rejection email");
                }

                return (true, "Refund rejected successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error rejecting refund {refundId}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region Get Refunds

        public async Task<List<Refund>> GetPendingRefundsAsync()
        {
            return await Task.Run(() => _unitOfWork.Refunds.GetPendingRefunds());
        }

        public async Task<Refund?> GetRefundByIdAsync(int refundId)
        {
            return await Task.Run(() => _unitOfWork.Refunds.GetById(refundId));
        }

        public async Task<Refund?> GetRefundByBookingIdAsync(int bookingId)
        {
            return await Task.Run(() => _unitOfWork.Refunds.GetByBookingId(bookingId));
        }

        #endregion
    }
}