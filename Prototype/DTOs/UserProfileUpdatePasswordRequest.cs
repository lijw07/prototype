namespace Prototype.DTOs;

public class UserProfileUpdatePasswordRequest
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}