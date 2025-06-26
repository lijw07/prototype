namespace Prototype.DTOs.Request;

public class UserRequestDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public string Role { get; set; } = "User";
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsTemporary { get; set; } = false;
}