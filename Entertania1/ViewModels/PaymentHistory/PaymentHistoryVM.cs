using Core.Enums;

namespace Entertania1.ViewModels.PaymentHistory
{
    /// <summary>
    /// ✅ ENHANCED: Support for status progression and better display logic
    /// </summary>
    public class PaymentHistoryVM
    {
        public string EventName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Description { get; set; }
        public decimal? OriginalAmount { get; set; }

        /// <summary>
        /// ✅ NEW: Status progression history
        /// </summary>
        public List<StatusHistoryItem> StatusHistory { get; set; } = new();

        /// <summary>
        /// Check if this is a refund transaction
        /// </summary>
        public bool IsRefund => TransactionType == "Refund";

        /// <summary>
        /// Check if this is a cancellation
        /// </summary>
        public bool IsCancellation => TransactionType == "Cancellation" ||
                                      (Amount == 0 && Status == PaymentStatus.Failed);

        /// <summary>
        /// Get display amount (absolute value)
        /// </summary>
        public decimal DisplayAmount => Math.Abs(Amount);

        /// <summary>
        /// ✅ ENHANCED: Beautiful formatted amount with refund calculation
        /// </summary>
        public string FormattedAmount
        {
            get
            {
                if (IsCancellation)
                {
                    return "$0.00";
                }

                if (IsRefund && OriginalAmount.HasValue && OriginalAmount > 0)
                {
                    var refundPercentage = (DisplayAmount / OriginalAmount.Value) * 100;
                    return $"${DisplayAmount:N2}";
                }

                if (IsRefund)
                {
                    return $"${DisplayAmount:N2}";
                }

                return $"${DisplayAmount:N2}";
            }
        }

        /// <summary>
        /// Get CSS class for amount color
        /// </summary>
        public string AmountColorClass => IsRefund ? "text-danger" : "text-success";

        /// <summary>
        /// ✅ UPDATED: Better status badge for all states
        /// </summary>
        public string StatusBadgeClass => Status switch
        {
            PaymentStatus.Pending when IsRefund => "badge bg-warning text-dark",
            PaymentStatus.Pending => "badge bg-warning text-dark",
            PaymentStatus.Processing when IsRefund => "badge bg-info text-white",
            PaymentStatus.Processing => "badge bg-info text-white",
            PaymentStatus.Completed => "badge bg-success",
            PaymentStatus.Refunded => "badge bg-primary",
            PaymentStatus.RefundRejected => "badge bg-danger",
            PaymentStatus.Failed when IsCancellation => "badge bg-secondary",
            PaymentStatus.Failed => "badge bg-danger",
            _ => "badge bg-secondary"
        };

        /// <summary>
        /// ✅ Customer-friendly status text
        /// </summary>
        public string StatusText => Status switch
        {
            PaymentStatus.Pending when IsRefund => "Refund Pending",
            PaymentStatus.Pending => "Payment Pending",
            PaymentStatus.Processing when IsRefund => "Refund Processing",
            PaymentStatus.Processing => "Payment Processing",
            PaymentStatus.Completed => "Payment Complete",
            PaymentStatus.Refunded => "Refund Complete",
            PaymentStatus.RefundRejected => "Refund Rejected",
            PaymentStatus.Failed when IsCancellation => "Booking Cancelled",
            PaymentStatus.Failed when IsRefund => "Refund Failed",
            PaymentStatus.Failed => "Payment Failed",
            _ => Status.ToString()
        };

        /// <summary>
        /// Get icon for transaction type
        /// </summary>
        public string Icon => IsCancellation ? "✕" : (IsRefund ? "↻" : "✓");

        /// <summary>
        /// Get user-friendly payment method text
        /// </summary>
        public string DisplayMethod
        {
            get
            {
                if (IsCancellation)
                {
                    if (Description?.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true)
                        return "Auto-Cancelled (Timeout)";
                    if (Description?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true)
                        return "Admin Action";
                    return "User Cancelled";
                }

                if (IsRefund)
                {
                    return Status switch
                    {
                        PaymentStatus.Refunded => "Refund Completed",
                        PaymentStatus.RefundRejected => "Refund Declined",
                        PaymentStatus.Processing => "Processing Refund",
                        PaymentStatus.Pending => "Refund Requested",
                        PaymentStatus.Failed => "Refund Failed",
                        _ => "Refund"
                    };
                }

                return PaymentMethod ?? "Paymob";
            }
        }

        /// <summary>
        /// ✅ NEW: Get refund info message
        /// </summary>
        public string RefundInfoMessage
        {
            get
            {
                if (!IsRefund || !OriginalAmount.HasValue || OriginalAmount <= 0)
                    return string.Empty;

                var refundPercentage = (DisplayAmount / OriginalAmount.Value) * 100;
                return $"80% of ${OriginalAmount:N2}";
            }
        }

        /// <summary>
        /// Get detailed description
        /// </summary>
        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Description))
                return Description;

            if (IsCancellation)
            {
                return "Booking was cancelled before payment completion. No charges were made.";
            }

            if (IsRefund)
            {
                return Status switch
                {
                    PaymentStatus.Pending => "Your refund request is pending admin approval.",
                    PaymentStatus.Processing => "Your refund is being processed by the payment gateway.",
                    PaymentStatus.Refunded => "Refund completed. Funds will appear in 5-7 business days.",
                    PaymentStatus.RefundRejected => "Your refund request was declined by the administrator.",
                    PaymentStatus.Failed => "Refund processing failed. Please contact support.",
                    _ => "Refund status unknown."
                };
            }

            return Status switch
            {
                PaymentStatus.Pending => "Payment initiated. Please complete the payment process.",
                PaymentStatus.Processing => "Your payment is being processed.",
                PaymentStatus.Completed => "Payment successful! Your booking is confirmed.",
                PaymentStatus.Failed => "Payment could not be processed.",
                _ => ""
            };
        }

        /// <summary>
        /// ✅ NEW: Get progression indicator
        /// </summary>
        public string GetProgressionSteps()
        {
            if (IsCancellation) return "❌ Cancelled";

            if (IsRefund)
            {
                return Status switch
                {
                    PaymentStatus.Pending => "1️⃣ Requested → ⏳ Pending Approval",
                    PaymentStatus.Processing => "1️⃣ Approved → 2️⃣ Processing → ⏳ Completing",
                    PaymentStatus.Refunded => "1️⃣ Requested → 2️⃣ Approved → 3️⃣ ✅ Completed",
                    PaymentStatus.RefundRejected => "1️⃣ Requested → ❌ Rejected",
                    PaymentStatus.Failed => "1️⃣ Requested → 2️⃣ Attempted → ❌ Failed",
                    _ => ""
                };
            }

            return Status switch
            {
                PaymentStatus.Pending => "1️⃣ Initiated → ⏳ Awaiting Payment",
                PaymentStatus.Processing => "1️⃣ Initiated → 2️⃣ Processing → ⏳ Confirming",
                PaymentStatus.Completed => "1️⃣ Initiated → 2️⃣ Processed → 3️⃣ ✅ Confirmed",
                PaymentStatus.Failed => "1️⃣ Initiated → ❌ Failed",
                _ => ""
            };
        }
    }

    /// <summary>
    /// ✅ NEW: Status history item for progression timeline
    /// </summary>
    public class StatusHistoryItem
    {
        public PaymentStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Description { get; set; }

        public string StatusText => Status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Processing => "Processing",
            PaymentStatus.Completed => "Completed",
            PaymentStatus.Refunded => "Refunded",
            PaymentStatus.RefundRejected => "Rejected",
            PaymentStatus.Failed => "Failed",
            _ => Status.ToString()
        };

        public string Icon => Status switch
        {
            PaymentStatus.Pending => "⏳",
            PaymentStatus.Processing => "⚙️",
            PaymentStatus.Completed => "✅",
            PaymentStatus.Refunded => "✅",
            PaymentStatus.RefundRejected => "❌",
            PaymentStatus.Failed => "❌",
            _ => "•"
        };
    }
}