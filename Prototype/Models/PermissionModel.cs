using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class PermissionModel
{
    [Key]
    public required Guid PermissionId { get; set; }
    
    [Required]
    public required string PermissionName { get; set; }
    
    [Required]
    public required string Description { get; set; }
    
    public ICollection<ApplicationModel> Applications { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}