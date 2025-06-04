using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class EmployeeRoleModel
{
    [Key]
    public required Guid EmployeeRoleId { get; set; }
    
    [Required]
    public required string JobTitle { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}