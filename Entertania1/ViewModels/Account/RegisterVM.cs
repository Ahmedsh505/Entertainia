using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Account
{
    public class RegisterVM
    {
        [Required]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Required, DataType(DataType.Text)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required, DataType(DataType.PhoneNumber)]
        [Display(Name = "PhoneNumber")]
        public string? PhoneNumber { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required, DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password), Compare(nameof(Password))]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Role")]
        public string? Role { get; set; } // "Customer" or "Organizer"

        // Updated: Changed to List<IFormFile> for multiple uploads
        [Display(Name = "Verification Documents")]
        public List<IFormFile>? VerificationDocuments { get; set; }
    }
}