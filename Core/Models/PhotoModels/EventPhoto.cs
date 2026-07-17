using System;
using System.ComponentModel.DataAnnotations;
using Core.Models.EventModels;

namespace Core.Models.PhotoModels
{
    /// <summary>
    /// Represents a photo in an event's gallery
    /// </summary>
    public class EventPhoto
    {
        public int EventPhotoId { get; set; }

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
        /// Is this the primary/cover photo for the event?
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        [StringLength(500)]
        public string? Caption { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int UploadedByUserId { get; set; }

        public User? UploadedBy { get; set; }

        // File metadata
        public long FileSize { get; set; }

        [StringLength(50)]
        public string? ContentType { get; set; }
    }
}