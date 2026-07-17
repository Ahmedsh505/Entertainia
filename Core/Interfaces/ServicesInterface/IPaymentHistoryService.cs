using Core.Enums;
using Core.Models;

namespace Core.Interfaces.ServicesInterface
{
    public interface IPaymentHistoryService
    {
        Task LogAsync(int userId, int bookingId, decimal amount, PaymentStatus status, string? method = null, string? transactionId = null);
        Task<IEnumerable<PaymentHistory>> GetUserPaymentsAsync(int userId);


    }
}
