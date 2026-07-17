using Core.Interfaces.RepositoriesInterface;
using Core.Models.BookingModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepo
    {
        private readonly EntertaniaDbcontext _context;

        public BookingRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Booking booking)
        {
            _context.Bookings.Add(booking);
        }

        public void Update(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public void Delete(Booking booking)
        {
            _context.Bookings.Remove(booking);
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<Booking> GetAll()
        {
            return _context.Bookings
                .IgnoreQueryFilters() // ✅ Load deleted venues
                .Include(b => b.User)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue) // ✅ Load venue even if deleted
                .ToList();
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public Booking GetById(int id)
        {
            return _context.Bookings
                .IgnoreQueryFilters() // ✅ Load deleted venues
                .Include(b => b.User)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue) // ✅ Load venue even if deleted
                .Include(b => b.Event)
                    .ThenInclude(e => e.Organizer) // ✅ Load organizer even if deleted
                .Include(b => b.DiscountCode)
                .FirstOrDefault(b => b.BookingId == id);
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<Booking> GetByUser(int userId)
        {
            return _context.Bookings
                .IgnoreQueryFilters() // ✅ Load deleted venues
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue) // ✅ Load venue even if deleted
                .Include(b => b.Event)
                    .ThenInclude(e => e.Organizer) // ✅ Load organizer even if deleted
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ToList();
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<Booking> GetByEvent(int eventId)
        {
            return _context.Bookings
                .IgnoreQueryFilters() // ✅ Load deleted venues
                .Include(b => b.User)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue) // ✅ Load venue even if deleted
                .Where(b => b.EventId == eventId)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}