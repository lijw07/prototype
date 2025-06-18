using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class UserModel
{
    [Key]
    public required Guid UserId { get; set; }
    
    [Required]
    public required string FirstName { get; set; }
    
    [Required]
    public required string LastName { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    // TODO(lijw07): Add support for permissions once roles are defined
    public Guid UserPermissionsId { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public required DateTime UpdatedAt { get; set; }
}