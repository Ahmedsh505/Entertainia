using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.EventModels;

namespace Core.Models.VideoModels
{
    /// <summary>
    /// Represents a video in an event's gallery
    /// </summary>
    public class EventVideo
    {
        public int EventVideoId { get; set; }

        [Required]
        public int EventId { get; set; }

        public EventBase? Event { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Display order in gallery (lower numbers shown first)
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Is this the primary/featured video for the event?
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        [StringLength(500)]
        public string? Caption { get; set; }

        /// <summary>
        /// Video duration in seconds
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// Thumbnail image path (auto-generated or uploaded)
        /// </summary>
        [StringLength(500)]
        public string? ThumbnailPath { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int UploadedByUserId { get; set; }

        public User? UploadedBy { get; set; }

        // File metadata
        public long FileSize { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Video resolution (e.g., "1920x1080", "1280x720")
        /// </summary>
        [StringLength(20)]
        public string? Resolution { get; set; }
    }
}