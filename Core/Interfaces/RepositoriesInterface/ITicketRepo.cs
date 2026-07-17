using Core.Models.TicketModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.RepositoriesInterface
{
    public interface ITicketRepo
    {
        void Add(Ticket ticket);
        void Update(Ticket ticket);
        void Delete(Ticket ticket);
        Ticket? GetById(int ticketId);
        Ticket? GetByVerificationHash(string hash);
        List<Ticket> GetByBookingId(int bookingId);
        List<Ticket> GetByEventId(int eventId);
        List<Ticket> GetByUserId(int userId);
        List<Ticket> GetActiveTickets();
        List<Ticket> GetExpiredTickets();
        void Save();
    }
}
