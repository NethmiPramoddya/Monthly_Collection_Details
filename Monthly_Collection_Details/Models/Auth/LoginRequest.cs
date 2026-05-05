using System.ComponentModel.DataAnnotations;

namespace Monthly_Collection_Details.Models.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Province code is required.")]
        [StringLength(3, MinimumLength = 1)]
        public string MyAddCode { get; set; }
    }
}


