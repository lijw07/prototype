using Prototype.Utility;

namespace Prototype.DTOs;

public class ForgotUserRequest
{
    public required string Email { get; set; }
    
    public UserRecoveryTypeEnum UserRecoveryType { get; set; }
}