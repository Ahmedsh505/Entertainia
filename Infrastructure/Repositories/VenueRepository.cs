using Core.Enums;
using Core.Interfaces.RepositoriesInterface;
using Core.Models;
using Infrastructure.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Repositories
{
    public class VenueRepository : IVenueRepo
    {
        private readonly EntertaniaDbcontext _context;

        public VenueRepository(EntertaniaDbcontext context)
        {
            _context = context;
        }

        public void Add(Venue venue)
        {
            _context.Venues.Add(venue);
        }

        public void Update(Venue venue)
        {
            _context.Venues.Update(venue);
        }

        /// <summary>
        /// ✅ FIXED: Soft delete instead of hard delete
        /// </summary>
        public void Delete(Venue venue)
        {
            // Check if venue has events
            var hasEvents = _context.Events.Any(e => e.VenueId == venue.VenueId);

            if (hasEvents)
            {
                // Soft delete: Mark as deleted but keep in database
                venue.IsDeleted = true;
                venue.DeletedAt = DateTime.UtcNow;
                venue.OriginalName = venue.OriginalName ?? venue.Name;
                venue.Name = $"[Deleted] {venue.OriginalName ?? venue.Name}";
                _context.Venues.Update(venue);
            }
            else
            {
                // No events: Safe to hard delete
                _context.Venues.Remove(venue);
            }
        }

        public List<Venue> GetAll()
        {
            return _context.Venues
                .IgnoreQueryFilters() // ✅ Include deleted venues for admin view
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .ToList();
        }

        public IQueryable<Venue> GetQuery()
        {
            return _context.Venues
                .Where(v => !v.IsDeleted); // ✅ Exclude deleted by default
        }

        public Venue GetById(int id)
        {
            return _context.Venues
                .IgnoreQueryFilters() // ✅ Allow loading deleted venues (for existing events)
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .FirstOrDefault(v => v.VenueId == id);
        }

        public Venue GetByIdWithEvents(int id)
        {
            return _context.Venues
                .IgnoreQueryFilters() // ✅ Allow loading deleted venues
                .Include(v => v.Events)
                    .ThenInclude(e => e.Organizer)
                .FirstOrDefault(v => v.VenueId == id);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        // ------------------------------
        // Approval workflow methods
        // ------------------------------

        public List<Venue> GetPendingVenues()
        {
            return _context.Venues
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .Where(v => !v.IsDeleted && v.ApprovalStatus == ApprovalStatus.Pending)
                .ToList();
        }

        public List<Venue> GetByUser(int organizerId)
        {
            return _context.Venues
                .IgnoreQueryFilters() // ✅ Show organizer their deleted venues
                .Where(v => v.CreatedByUserId == organizerId)
                .ToList();
        }

        public List<Venue> GetApprovedVenues()
        {
            return _context.Venues
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .Where(v => !v.IsDeleted && v.ApprovalStatus == ApprovalStatus.Approved)
                .ToList();
        }

        public List<Venue> GetByGovernorate(Governorate governorate)
        {
            return _context.Venues
                .Where(v => !v.IsDeleted && v.Governorate == governorate)
                .Include(v => v.CreatedByUser)
                .ToList();
        }

        public void SetApprovalStatus(int venueId, ApprovalStatus status, int? approvedById = null, string? note = null)
        {
            var venue = _context.Venues.FirstOrDefault(v => v.VenueId == venueId);
            if (venue != null)
            {
                venue.ApprovalStatus = status;
                venue.ApprovedByUserId = approvedById;
                venue.ApprovalNote = note;
                venue.ApprovedAt = status == ApprovalStatus.Approved ? DateTime.UtcNow : null;
                _context.Venues.Update(venue);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// ✅ Soft delete venue (marks as deleted but preserves for existing events)
        /// </summary>
        public void SoftDeleteVenue(int venueId, int deletedByUserId)
        {
            var venue = _context.Venues
                .Include(v => v.Events)
                .IgnoreQueryFilters()
                .FirstOrDefault(v => v.VenueId == venueId);

            if (venue == null) return;

            // Check for future events
            var now = DateTime.UtcNow;
            var hasFutureEvents = venue.Events.Any(e =>
                e.StartDate > now &&
                e.ApprovalStatus == ApprovalStatus.Approved);

            if (hasFutureEvents)
            {
                throw new InvalidOperationException(
                    "Cannot delete venue with future approved events. Cancel or move events first.");
            }

            // Soft delete
            venue.IsDeleted = true;
            venue.DeletedAt = DateTime.UtcNow;
            venue.DeletedByUserId = deletedByUserId;
            venue.OriginalName = venue.OriginalName ?? venue.Name;
            venue.Name = $"[Deleted] {venue.OriginalName}";

            _context.Venues.Update(venue);
            _context.SaveChanges();
        }

        /// <summary>
        /// ✅ Get only active (non-deleted) venues for public listings
        /// </summary>
        public List<Venue> GetActiveVenues()
        {
            return _context.Venues
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .Where(v => !v.IsDeleted && v.ApprovalStatus == ApprovalStatus.Approved)
                .ToList();
        }

        /// <summary>
        /// ✅ Get venue by ID including deleted (for event display)
        /// </summary>
        public Venue GetByIdIncludingDeleted(int id)
        {
            return _context.Venues
                .IgnoreQueryFilters()
                .Include(v => v.CreatedByUser)
                .Include(v => v.ApprovedBy)
                .FirstOrDefault(v => v.VenueId == id);
        }
    }
}