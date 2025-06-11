using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class EmployeeModel
{
    [Key]
    public required Guid EmployeeId { get; set; }
    
    [Required]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public ApplicationModel Application { get; set; }
    
    [Required]
    public required string FirstName { get; set; }
    
    [Required]
    public required string LastName { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    [Required]
    public required string JobTitle { get; set; }

    [Required]
    public required string Department { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public EmployeePermissionTypeEnum EmployeePermissionType { get; set; }
    
    public required string Manager { get; set; }
    
    [Required]
    public required DateTime HireDate { get; set; }
    
    [Required]
    public DateTime TerminationDate { get; set; }
}