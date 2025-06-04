using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class EmployeeModel
{
    [Key]
    public required Guid EmployeeId { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }
    
    [Required]
    public required string FirstName { get; set; }
    
    [Required]
    public required string LastName { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    [Required]
    public required string Manager { get; set; }
    
    [Required]
    public required string Department { get; set; }
    
    [Required]
    public required string Location { get; set; }
    
    [Required]
    public required string JobTitle { get; set; }
    
    [Required]
    public required Guid EmployeePermissionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(EmployeePermissionId))]
    public required EmployeePermissionModel EmployeePermission { get; set; }
    
    [Required]
    public required Guid EmployeeRoleId { get; set; }
    
    [Required]
    [ForeignKey("EmployeeRoleId")]
    public required EmployeeRoleModel EmployeeRole { get; set; }
    
    public ICollection<EmployeeApplicationModel> EmployeeApplications { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}