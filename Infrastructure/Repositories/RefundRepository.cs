using Core.Enums;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.PaymentModels;

namespace Infrastructure.Repositories
{
    public class RefundRepository : IRefundRepo
    {
        private readonly EntertaniaDbcontext _context;

        public RefundRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Refund refund)
        {
            _context.Set<Refund>().Add(refund);
        }

        public void Update(Refund refund)
        {
            _context.Set<Refund>().Update(refund);
        }

        public void Delete(Refund refund)
        {
            _context.Set<Refund>().Remove(refund);
        }

        public List<Refund> GetAll()
        {
            return _context.Set<Refund>()
                .Include(r => r.Payment)
                    .ThenInclude(p => p.Booking)
                        .ThenInclude(b => b.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Event)
                .Include(r => r.RequestedBy)
                .Include(r => r.ProcessedBy)
                .ToList();
        }

        public Refund? GetById(int id)
        {
            return _context.Set<Refund>()
                .Include(r => r.Payment)
                    .ThenInclude(p => p.Booking)
                        .ThenInclude(b => b.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Event)
                .Include(r => r.RequestedBy)
                .Include(r => r.ProcessedBy)
                .FirstOrDefault(r => r.RefundId == id);
        }

        public Refund? GetByBookingId(int bookingId)
        {
            return _context.Set<Refund>()
                .Include(r => r.Payment)
                .Include(r => r.Booking)
                .FirstOrDefault(r => r.BookingId == bookingId);
        }

        public Refund? GetByPaymentId(int paymentId)
        {
            return _context.Set<Refund>()
                .Include(r => r.Payment)
                .Include(r => r.Booking)
                .FirstOrDefault(r => r.PaymentId == paymentId);
        }

        public List<Refund> GetByStatus(PaymentStatus status)
        {
            return _context.Set<Refund>()
                .Include(r => r.Payment)
                    .ThenInclude(p => p.Booking)
                        .ThenInclude(b => b.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Event)
                .Where(r => r.Status == status)
                .ToList();
        }

        public List<Refund> GetPendingRefunds()
        {
            return GetByStatus(PaymentStatus.Pending);
        }

        public void UpdateStatus(int refundId, PaymentStatus status)
        {
            var refund = _context.Set<Refund>().Find(refundId);
            if (refund != null)
            {
                refund.Status = status;

                if (status == PaymentStatus.Completed || status == PaymentStatus.Refunded)
                    refund.CompletedAt = DateTime.UtcNow;
                else if (status == PaymentStatus.Failed)
                    refund.FailedAt = DateTime.UtcNow;
                else if (status == PaymentStatus.Processing)
                    refund.ProcessedAt = DateTime.UtcNow;

                _context.Set<Refund>().Update(refund);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}