using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class EmployeeRoleModel
{
    [Key]
    public Guid EmployeeRoleId { get; set; }
    
    [Required]
    public string JobTitle { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}