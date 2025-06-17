using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class UserPermissionModel
{
    [Key]
    public required Guid UserPermissionId { get; set; }

    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required string CreatedBy { get; set; }
}