using Prototype.Enum;

namespace Prototype.DTOs;

public class ForgotUserRequestDto
{
    public required string Email { get; set; }
    
    public UserRecoveryTypeEnum UserRecoveryType { get; set; }
}