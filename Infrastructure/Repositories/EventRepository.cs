using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Models.EventModels;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Repositories
{
    public class EventRepository : IEventRepo
    {
        private readonly EntertaniaDbcontext _context;

        public EventRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(EventBase ev)
        {
            _context.Events.Add(ev);
        }

        public void Update(EventBase ev)
        {
            var existingEntry = _context.ChangeTracker.Entries<EventBase>()
                .FirstOrDefault(e => e.Entity.EventId == ev.EventId);

            if (existingEntry != null)
            {
                existingEntry.CurrentValues.SetValues(ev);
                existingEntry.State = EntityState.Modified;
            }
            else
            {
                _context.Events.Update(ev);
            }
        }

        public void Delete(EventBase ev)
        {
            _context.Events.Remove(ev);
        }

        public void Detach(EventBase entity)
        {
            var entry = _context.Entry(entity);
            if (entry != null && entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }

        public async Task<List<EventBase>> GetBookedEventsByVenueAsync(int venueId)
        {
            return await _context.Events
                .Where(e => e.VenueId == venueId && e.ApprovalStatus == ApprovalStatus.Approved)
                .Select(e => new EventBase
                {
                    EventId = e.EventId,
                    Title = e.Title,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    VenueId = e.VenueId
                })
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// ✅ FIXED: Load venues with IgnoreQueryFilters to include deleted venues
        /// </summary>
        public List<EventBase> GetAll()
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToList();
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public EventBase GetById(int id)
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Bookings)  // ✅ Keep for single event
                .AsSplitQuery()  // ✅ ADD THIS
                .FirstOrDefault(e => e.EventId == id);
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public EventBase GetByIdNoTracking(int id)
        {
            return _context.Events
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.Bookings)  // ✅ Keep for single event
                .AsSplitQuery()  // ✅ ADD THIS
                .FirstOrDefault(e => e.EventId == id);
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<EventBase> GetByCategory(string category)
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .Where(e => e.Category == category)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToList();
        }

        public async Task<List<EventBase>> GetTrendingOrUpcomingEventsAsync(int count = 5)
        {
            var now = DateTime.Now;

            // 1️⃣ جلب جميع الأحداث المعتمدة واللي لسه ما بدأت
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Bookings)
                .Where(e => e.ApprovalStatus == ApprovalStatus.Approved && e.StartDate >= now)
                .ToListAsync();

            // 2️⃣ حساب المقاعد المتاحة
            foreach (var ev in events)
            {
                var bookedSeats = ev.Bookings?.Where(b => b.Status != "Cancelled").Sum(b => b.NumberOfTickets) ?? 0;
                var venueCapacity = ev.Venue?.Capacity ?? 0;
            }

            // 3️⃣ ترتيب الترند حسب عدد التذاكر المباعة
            var trending = events
                .OrderByDescending(e => e.Bookings?.Count(b => b.Status != "Cancelled") ?? 0)
                .Take(count)
                .ToList();

            // 4️⃣ لو ما فيش أحداث متباعة، عرض أول n أحداث قادمة
            if (!trending.Any())
            {
                trending = events
                    .OrderBy(e => e.StartDate >= now)
                    .Take(count)
                    .ToList();
            }

            return trending;
        }


        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<EventBase> GetByOrganizer(int organizerId)
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .Where(e => e.OrganizerId == organizerId)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        // ------------------------------
        // Approval workflow methods
        // ------------------------------

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<EventBase> GetPendingEvents()
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .Where(e => e.ApprovalStatus == ApprovalStatus.Pending)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToList();
        }

        /// <summary>
        /// ✅ FIXED: Load venues including deleted ones
        /// </summary>
        public List<EventBase> GetApprovedEvents()
        {
            return _context.Events
                .IgnoreQueryFilters()
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .Where(e => e.ApprovalStatus == ApprovalStatus.Approved)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToList();
        }

        public void SetApprovalStatus(int eventId, ApprovalStatus status, int? approvedById = null, string? note = null)
        {
            var ev = _context.Events.FirstOrDefault(e => e.EventId == eventId);
            if (ev != null)
            {
                ev.ApprovalStatus = status;
                ev.ApprovedByUserId = approvedById;
                ev.ApprovalNote = note;
                ev.ApprovedAt = status == ApprovalStatus.Approved ? DateTime.UtcNow : null;
                _context.Events.Update(ev);
                _context.SaveChanges();
            }
        }

        // ✅ 1. كل الأحداث
        public async Task<List<EventBase>> GetAllEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToListAsync();
        }

        // ✅ 2. أحداث الأورجنايزر
        public async Task<List<EventBase>> GetEventsByOrganizerAsync(int organizerId)
        {
            return await _context.Events
                .Include(e => e.Venue)
                // ❌ REMOVE: .Include(e => e.Bookings)
                .Where(e => e.OrganizerId == organizerId)
                .AsSplitQuery()  // ✅ ADD THIS
                .AsNoTracking()  // ✅ ADD THIS
                .ToListAsync();
        }

        // ✅ 3. الأحداث المعتمدة فقط (Approved)
        public async Task<List<EventBase>> GetApprovedEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                // ✅ REMOVED: .Include(e => e.Bookings) - Not needed for listing
                .Where(e => e.ApprovalStatus == Core.Enums.ApprovalStatus.Approved)
                .AsSplitQuery()  // ✅ Split into separate queries
                .AsNoTracking()  // ✅ Faster - no change tracking needed
                .ToListAsync();
        }




    }
}