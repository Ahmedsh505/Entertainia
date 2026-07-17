using Core.Models.PaymentModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.ServicesInterface
{
    public interface IRefundService
    {

         // ===== REQUEST REFUND =====
        Task<(bool Success, string Message, Refund? Refund)> RequestRefundAsync(int bookingId, int userId, string reason);

        // ===== APPROVE/REJECT REFUND =====
        Task<(bool Success, string Message)> ApproveRefundAsync(int refundId, int adminUserId, string? note = null);
        Task<(bool Success, string Message)> RejectRefundAsync(int refundId, int adminUserId, string reason);

        // ===== GET REFUNDS =====
        Task<List<Refund>> GetPendingRefundsAsync();
        Task<Refund?> GetRefundByIdAsync(int refundId);
        Task<Refund?> GetRefundByBookingIdAsync(int bookingId);

    }
}
