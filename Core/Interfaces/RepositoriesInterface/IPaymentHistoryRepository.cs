using Core.Models;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface IPaymentHistoryRepository
    {
        Task<IEnumerable<PaymentHistory>> GetByUserIdAsync(int userId);
        Task<IEnumerable<PaymentHistory>> GetByBookingIdAsync(int bookingId);
        Task AddAsync(PaymentHistory entity);

        /// <summary>
        /// ✅ NEW: Update existing payment history record
        /// </summary>
        Task UpdateAsync(PaymentHistory entity);

        /// <summary>
        /// ✅ NEW: Delete payment history record
        /// </summary>
        Task DeleteAsync(PaymentHistory entity);
    }
}