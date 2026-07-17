using Core.Enums;
using Core.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Venue
{
    public class VenueCreateViewModel
    {
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(500)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, 1000000, ErrorMessage = "Capacity must be between 1 and 1,000,000")]
        public int Capacity { get; set; }

        // ✅ Governorate with custom validation
        [Required(ErrorMessage = "Governorate is required")]
        [Display(Name = "Governorate")]
        [ValidGovernorate(ErrorMessage = "Selected governorate doesn't match the location coordinates")]
        public Governorate Governorate { get; set; } = Governorate.Cairo;

        // ✅ NEW: Google Maps Integration
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double? Longitude { get; set; }

        [StringLength(1000)]
        public string? GoogleMapsUrl { get; set; }

        // Helper: Auto-generate Google Maps URL from coordinates
        public void GenerateGoogleMapsUrl()
        {
            if (Latitude.HasValue && Longitude.HasValue)
            {
                GoogleMapsUrl = $"https://www.google.com/maps?q={Latitude.Value},{Longitude.Value}";
            }
        }
    }
}