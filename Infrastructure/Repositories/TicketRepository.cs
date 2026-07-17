using Core.Interfaces.RepositoriesInterface;
using Core.Models.TicketModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class TicketRepository : ITicketRepo
    {
        private readonly EntertaniaDbcontext _context;

        public TicketRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
        }

        public void Update(Ticket ticket)
        {
            _context.Tickets.Update(ticket);
        }

        public void Delete(Ticket ticket)
        {
            _context.Tickets.Remove(ticket);
        }

        // ✅ OPTIMIZED: Minimal includes for verification page
        public Ticket? GetById(int ticketId)
        {
            return _context.Tickets
                .Include(t => t.Event)
                    .ThenInclude(e => e.Venue) // Only Venue needed
                .Include(t => t.User)
                .Include(t => t.Booking)
                .AsNoTracking() // Read-only performance boost
                .FirstOrDefault(t => t.TicketId == ticketId);
        }

        public Ticket? GetByVerificationHash(string hash)
        {
            return _context.Tickets
                .Include(t => t.Event)
                    .ThenInclude(e => e.Venue)
                .Include(t => t.User)
                .Include(t => t.Booking)
                .AsNoTracking()
                .FirstOrDefault(t => t.VerificationHash == hash);
        }

        public List<Ticket> GetByBookingId(int bookingId)
        {
            return _context.Tickets
                .Include(t => t.Event)
                    .ThenInclude(e => e.Venue) // Include Venue for PDF
                .Include(t => t.User) // Include User for PDF
                .AsNoTracking()
                .Where(t => t.BookingId == bookingId)
                .OrderBy(t => t.TicketNumber)
                .ToList();
        }

        public List<Ticket> GetByEventId(int eventId)
        {
            return _context.Tickets
                .Include(t => t.Booking)
                .Include(t => t.User)
                .AsNoTracking()
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.TicketNumber)
                .ToList();
        }

        public List<Ticket> GetByUserId(int userId)
        {
            return _context.Tickets
                .Include(t => t.Event)
                .Include(t => t.Booking)
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();
        }

        public List<Ticket> GetActiveTickets()
        {
            return _context.Tickets
                .AsNoTracking()
                .Where(t => t.Status == "Active" &&
                           (!t.ExpiresAt.HasValue || t.ExpiresAt.Value > DateTime.UtcNow))
                .ToList();
        }

        public List<Ticket> GetExpiredTickets()
        {
            return _context.Tickets
                .AsNoTracking()
                .Where(t => t.ExpiresAt.HasValue && t.ExpiresAt.Value <= DateTime.UtcNow)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}