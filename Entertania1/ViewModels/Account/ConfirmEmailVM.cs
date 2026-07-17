using System.ComponentModel.DataAnnotations;

namespace Entertainia.ViewModels.Account
{
    public class ConfirmEmailVM
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must contain only numbers")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        // Hidden field to store email (for security and user context)
        public string? Email { get; set; }
    }
}
