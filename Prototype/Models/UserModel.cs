using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class UserModel
{
    [Key]
    public required Guid UserId { get; set; }
    
    [Required]
    public required  string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }
    
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
    
    public required ICollection<ApplicationModel> Application { get; set; }
    
    public required ICollection<ActiveDirectoryModel> ActiveDirectory { get; set; }
    
    public required ICollection<AuditLogModel> AuditLog { get; set; }
    
    public required Guid UserSessionId { get; set; }
    
    [ForeignKey(nameof(UserSessionId))]
    public required UserSessionModel UserSession { get; set; }
    
    [Required]
    public required Guid HumanResourceId { get; set; }
    
    [Required]
    [ForeignKey(nameof(HumanResourceId))]
    public required HumanResourceModel HumanResource { get; set; }
    
    [Required]
    public required PermissionEnum Permission { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}