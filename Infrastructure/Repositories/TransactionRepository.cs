using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.PaymentModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepo
    {
        private readonly EntertaniaDbcontext _context;

        public TransactionRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Transaction transaction)
        {
            _context.Set<Transaction>().Add(transaction);
        }

        public void Update(Transaction transaction)
        {
            _context.Set<Transaction>().Update(transaction);
        }

        public void Delete(Transaction transaction)
        {
            _context.Set<Transaction>().Remove(transaction);
        }

        public Transaction? GetById(int id)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Event)
                .Include(t => t.Payment)
                .Include(t => t.Refund)
                .FirstOrDefault(t => t.TransactionId == id);
        }

        public Transaction? GetByReference(string transactionReference)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .FirstOrDefault(t => t.TransactionReference == transactionReference);
        }

        public Transaction? GetByGatewayTransactionId(string gatewayTransactionId)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .FirstOrDefault(t => t.GatewayTransactionId == gatewayTransactionId);
        }

        public List<Transaction> GetAll()
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Event)
                .Include(t => t.Payment)
                .Include(t => t.Refund)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByUser(int userId)
        {
            return _context.Set<Transaction>()
                .Include(t => t.Booking)
                    .ThenInclude(b => b.Event)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByBooking(int bookingId)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Payment)
                .Include(t => t.Refund)
                .Where(t => t.BookingId == bookingId)
                .OrderBy(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByPayment(int paymentId)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .Where(t => t.PaymentId == paymentId)
                .OrderBy(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByRefund(int refundId)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .Where(t => t.RefundId == refundId)
                .OrderBy(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByStatus(PaymentStatus status)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByType(string transactionType)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .Where(t => t.TransactionType == transactionType)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public List<Transaction> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Set<Transaction>()
                .Include(t => t.User)
                .Include(t => t.Booking)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
