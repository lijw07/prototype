using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs;

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Current password is required")]
    public required string CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    public required string NewPassword { get; set; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public required string ReTypeNewPassword { get; set; }
}