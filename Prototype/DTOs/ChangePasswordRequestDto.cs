using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Prototype.DTOs;

public class ChangePasswordRequestDto
{
    [Required(ErrorMessage = "Current password is required")]
    [JsonPropertyName("currentPassword")]
    public required string CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&]).{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character")]
    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; set; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    [JsonPropertyName("reTypeNewPassword")]
    public required string ReTypeNewPassword { get; set; }
}