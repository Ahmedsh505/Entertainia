using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Account
{
    public class Verify2FAVM
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}