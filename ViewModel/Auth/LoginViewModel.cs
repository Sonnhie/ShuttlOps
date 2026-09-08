using System.ComponentModel.DataAnnotations;

namespace ShuttlOps.ViewModel.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 5, ErrorMessage = "Username must be between 5 and 50 characters.")]
        public string UsernameInput { get; set; } = string.Empty;


        [Required(ErrorMessage = "Password Required.")]
        public string PasswordInput { get; set; } = string.Empty;
    }
}
