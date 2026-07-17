using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Account
{
    public class LoginVM
    {
        [Required]
        public string Identifier { get; set; } // email or username in one field

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

}
