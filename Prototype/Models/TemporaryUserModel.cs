using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class TemporaryUserModel
{
    [Key]
    public required Guid TemporaryUserId { get; set; }
    
    [Required]
    public required string FirstName { get; set; }
    
    [Required]
    public required string LastName { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    [Required]
    public required string Token { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
}