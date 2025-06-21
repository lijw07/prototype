using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs;

public class ImportUserDto
{
    public required string UserId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public string? LastLogin { get; set; }
    public bool? IsActive { get; set; }
    public string Role { get; set; } = "User";
}