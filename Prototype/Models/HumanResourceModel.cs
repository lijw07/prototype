using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class HumanResourceModel
{
    [Key]
    public required Guid HumanResourceId { get; set; }
    
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
    
    [Required]
    public required Guid EmployeeNumber { get; set; }
    
    [Required]
    public required JobPositionEnum JobTitle { get; set; }
    
    [Required]
    public required DepartmentEnum Department { get; set; }
    
    public Guid Manager { get; set; }
    
    [Required]
    public required string Location { get; set; }
    
    [Required]
    public required DateTime HireDate { get; set; }
    
    public DateTime TerminationDate { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
}