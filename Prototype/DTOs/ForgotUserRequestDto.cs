using System.ComponentModel.DataAnnotations;
using Prototype.Enum;

namespace Prototype.DTOs;

public class ForgotUserRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public required string Email { get; set; }
    
    [Required(ErrorMessage = "Recovery type is required")]
    public UserRecoveryTypeEnum UserRecoveryType { get; set; }
}