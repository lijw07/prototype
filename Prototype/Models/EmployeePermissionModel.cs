using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class EmployeePermissionModel
{
    [Key]
    public Guid EmployeePermissionId { get; set; }
    
    [Required]
    public PermissionEnum Permission { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}