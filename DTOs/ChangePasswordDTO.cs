using System.ComponentModel.DataAnnotations;

namespace ShuttlOps.DTOs
{
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters.")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
