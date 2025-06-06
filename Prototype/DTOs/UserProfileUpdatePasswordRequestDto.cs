namespace Prototype.DTOs;

public class UserProfileUpdatePasswordRequestDto
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}