using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class UserRoleModel
{
    [Key]
    public required Guid UserRoleId { get; set; }
    
    [Required]
    public required string Role { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    [Required]
    public required string CreatedBy { get; set; }
}