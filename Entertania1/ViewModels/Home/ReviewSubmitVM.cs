using System.ComponentModel.DataAnnotations;

namespace Entertainia.ViewModels.Home
{
    public class ReviewSubmitVM
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        [MinLength(10, ErrorMessage = "Comment must be at least 10 characters")]
        public string Comment { get; set; } = string.Empty;
    }
}