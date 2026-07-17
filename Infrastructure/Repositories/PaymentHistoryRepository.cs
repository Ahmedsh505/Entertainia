using Core.Interfaces.RepositoriesInterface;
using Core.Models;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class PaymentHistoryRepository : IPaymentHistoryRepository
    {
        private readonly EntertaniaDbcontext _context;

        public PaymentHistoryRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaymentHistory>> GetByUserIdAsync(int userId)
        {
            return await _context.PaymentHistories
                .Include(p => p.Booking)
                .ThenInclude(b => b.Event)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<PaymentHistory>> GetByBookingIdAsync(int bookingId)
        {
            return await _context.PaymentHistories
                .Include(p => p.User)
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(PaymentHistory entity)
        {
            await _context.PaymentHistories.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// ✅ NEW: Update existing payment history record
        /// </summary>
        public async Task UpdateAsync(PaymentHistory entity)
        {
            _context.PaymentHistories.Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// ✅ NEW: Delete payment history record (for cancelled unpaid bookings)
        /// </summary>
        public async Task DeleteAsync(PaymentHistory entity)
        {
            _context.PaymentHistories.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}