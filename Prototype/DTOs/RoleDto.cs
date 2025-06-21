namespace Prototype.DTOs;

public class RoleDto
{
    public Guid UserRoleId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}