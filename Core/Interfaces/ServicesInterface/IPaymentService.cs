using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Models.PaymentModels;
using Core.Models.BookingModels;

namespace Core.Interfaces.ServicesInterface
{
    public interface IPaymentService
    {
        // ===== PAYMENT INITIATION & PROCESSING =====
        Task<Payment> InitiatePaymentAsync(Booking booking);
        Task<bool> ProcessPaymentAsync(int paymentId, string transactionId);
        Task<bool> CompletePaymentAsync(int paymentId);
        Task<bool> FailPaymentAsync(int paymentId, string reason);

        // ✅ NEW: Mark payment as refunded
        Task<bool> RefundPaymentAsync(int paymentId, decimal refundAmount);

        // ===== PAYMENT RETRIEVAL =====
        Task<Payment?> GetPaymentByIdAsync(int paymentId);
        Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);

        // ===== PAYMOB INTEGRATION =====
        Task<string> GeneratePaymentUrlAsync(Payment payment);
        Task<bool> VerifyPaymentCallbackAsync(string transactionId, string signature);
        Task<bool> VerifyPaymentCallbackAsync(Dictionary<string, string> callbackData);

        // ===== ORGANIZER DASHBOARD HELPERS =====
        Task<decimal> GetTotalRevenueForOrganizerAsync(int organizerId);
        Task<decimal> GetMonthlyRevenueForOrganizerAsync(int organizerId, int year, int month);
        Task<int> GetTotalTicketsSoldForOrganizerAsync(int organizerId);
        Task<int> GetMonthlyTicketsSoldForOrganizerAsync(int organizerId, int year, int month);
        Task<(decimal TotalRevenue, decimal TotalTax, decimal TotalDiscount)> GetOrganizerFinancialsAsync(int organizerId);
    }
}