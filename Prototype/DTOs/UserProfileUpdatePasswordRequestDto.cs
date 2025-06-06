namespace Prototype.DTOs;

public class UserProfileUpdatePasswordRequestDto
{
    public required string OldPassword { get; set; }
    public required string NewPassword { get; set; }
}