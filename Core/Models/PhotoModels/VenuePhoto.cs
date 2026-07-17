using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.PhotoModels
{
    /// <summary>
    /// Represents a photo in a venue's gallery
    /// </summary>
    public class VenuePhoto
    {
        public int VenuePhotoId { get; set; }

        [Required]
        public int VenueId { get; set; }

        public Venue? Venue { get; set; }

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
        /// Is this the primary/cover photo for the venue?
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