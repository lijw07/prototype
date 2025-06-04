using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class EmployeeModel
{
    [Key]
    public Guid EmployeeId { get; set; }
    
    [Required]
    public string Username { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [Required]
    public string FirstName { get; set; }
    
    [Required]
    public string LastName { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string PhoneNumber { get; set; }
    
    [Required]
    public string Manager { get; set; }
    
    [Required]
    public string Department { get; set; }
    
    [Required]
    public string Location { get; set; }
    
    [Required]
    public string JobTitle { get; set; }
    
    [Required]
    public Guid EmployeePermissionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(EmployeePermissionId))]
    public EmployeePermissionModel EmployeePermission { get; set; }
    
    [Required]
    public Guid EmployeeRoleId { get; set; }
    
    [Required]
    [ForeignKey("EmployeeRoleId")]
    public EmployeeRoleModel EmployeeRole { get; set; }
    
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public ApplicationModel Application { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}