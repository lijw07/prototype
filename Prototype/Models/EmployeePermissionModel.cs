using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class EmployeePermissionModel
{
    [Key]
    public required Guid EmployeePermissionId { get; set; }
    
    [Required]
    public required PermissionEnum Permission { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}