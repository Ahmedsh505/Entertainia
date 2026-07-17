using Core.Enums;
using Core.Models.EventModels;
using Core.Models.PhotoModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Core.Models
{
    public class Venue
    {
        public int VenueId { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public int Capacity { get; set; }

        // ✅ NEW: Governorate field
        [Required(ErrorMessage = "Governorate is required")]
        public Governorate Governorate { get; set; }

        // ✅ NEW: Google Maps Integration
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(1000)]
        public string? GoogleMapsUrl { get; set; }

        // Approval & audit fields
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

        // Creator (organizer) - FK to ApplicationUser.Id
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        // Approver info
        public int? ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNote { get; set; }

        // ✅ NEW: Soft Delete properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted this venue (organizer who owned it)
        /// </summary>
        public int? DeletedByUserId { get; set; }

        /// <summary>
        /// User who deleted this venue
        /// </summary>
        public User? DeletedBy { get; set; }

        /// <summary>
        /// Original venue name before deletion (for reference in old events)
        /// </summary>
        public string? OriginalName { get; set; }

        // Convenience computed property
        public bool IsPublished => ApprovalStatus == ApprovalStatus.Approved && !IsDeleted;

        // Initialize collection
        public ICollection<EventBase> Events { get; set; } = new List<EventBase>();
        public ICollection<VenuePhoto> Photos { get; set; } = new List<VenuePhoto>();

        // ✅ Helper: Check if venue is currently occupied
        public bool IsCurrentlyOccupied()
        {
            var now = DateTime.UtcNow;
            return Events.Any(e =>
                e.ApprovalStatus == ApprovalStatus.Approved &&
                e.StartDate <= now &&
                e.EndDate >= now);
        }

        public EventBase? GetCurrentEvent()
        {
            var now = DateTime.UtcNow;
            return Events.FirstOrDefault(e =>
                e.ApprovalStatus == ApprovalStatus.Approved &&
                e.StartDate <= now &&
                e.EndDate >= now);
        }

        // ✅ Updated: Don't allow scheduling in deleted venues
        public bool HasScheduleConflict(DateTime startDate, DateTime endDate, int? excludeEventId = null)
        {
            // Can't schedule in deleted venues
            if (IsDeleted) return true;

            return Events.Any(e =>
                e.ApprovalStatus == ApprovalStatus.Approved &&
                (!excludeEventId.HasValue || e.EventId != excludeEventId.Value) &&
                (
                    (startDate >= e.StartDate && startDate < e.EndDate) || // New event starts during existing
                    (endDate > e.StartDate && endDate <= e.EndDate) ||     // New event ends during existing
                    (startDate <= e.StartDate && endDate >= e.EndDate)     // New event encompasses existing
                ));
        }

        // ✅ Helper: Can this venue be used for new events?
        public bool CanBeUsedForNewEvents()
        {
            return IsPublished && !IsDeleted;
        }

        // ✅ Helper: Get display name (shows "Deleted: Name" for deleted venues)
        public string GetDisplayName()
        {
            if (IsDeleted)
                return $"[Deleted] {OriginalName ?? Name}";
            return Name ?? "Unknown Venue";
        }
    }
}