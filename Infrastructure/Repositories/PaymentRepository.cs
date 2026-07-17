using Core.Enums;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.PaymentModels;

namespace Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepo
    {
        private readonly EntertaniaDbcontext _context;

        public PaymentRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Payment payment)
        {
            _context.Set<Payment>().Add(payment);
        }

        public void Update(Payment payment)
        {
            _context.Set<Payment>().Update(payment);
        }

        public void Delete(Payment payment)
        {
            _context.Set<Payment>().Remove(payment);
        }

        public List<Payment> GetAll()
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .ToList();
        }

        public Payment? GetById(int id)
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .FirstOrDefault(p => p.PaymentId == id);
        }

        public Payment? GetByBookingId(int bookingId)
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                .FirstOrDefault(p => p.BookingId == bookingId);
        }

        public Payment? GetByTransactionId(string transactionId)
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                .FirstOrDefault(p => p.TransactionId == transactionId);
        }

        public List<Payment> GetByStatus(PaymentStatus status)
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Status == status)
                .ToList();
        }

        public List<Payment> GetByUser(int userId)
        {
            return _context.Set<Payment>()
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Booking.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public void UpdateStatus(int paymentId, PaymentStatus status)
        {
            var payment = _context.Set<Payment>().Find(paymentId);
            if (payment != null)
            {
                payment.Status = status;

                if (status == PaymentStatus.Completed)
                    payment.CompletedAt = DateTime.UtcNow;
                else if (status == PaymentStatus.Failed)
                    payment.FailedAt = DateTime.UtcNow;
                else if (status == PaymentStatus.Processing)
                    payment.ProcessedAt = DateTime.UtcNow;

                _context.Set<Payment>().Update(payment);
            }
        }


        // ===================== DASHBOARD HELPERS =====================

        public decimal GetTotalRevenueForOrganizer(int organizerId)
        {
            return _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Booking.Event.OrganizerId == organizerId &&
                            p.Status == PaymentStatus.Completed)
                .Sum(p => (decimal?)p.Amount) ?? 0m;
        }

        public decimal GetMonthlyRevenueForOrganizer(int organizerId, int year, int month)
        {
            return _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Event)
                .Where(p => p.Booking.Event.OrganizerId == organizerId &&
                            p.Status == PaymentStatus.Completed &&
                            p.CreatedAt.Year == year &&
                            p.CreatedAt.Month == month)
                .Sum(p => (decimal?)p.Amount) ?? 0m;
        }


        public void Save()
        {
            _context.SaveChanges();
        }
    }
}