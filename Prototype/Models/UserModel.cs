using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class UserModel
{
    [Key]
    public required Guid UserId { get; set; }
    
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
    
    public required string Manager { get; set; }
    
    public required string Department { get; set; }
    
    public required string Location { get; set; }
    
    public required string JobTitle { get; set; }
    
    public ICollection<ApplicationModel> Application { get; set; }
    
    public ICollection<ActiveDirectoryModel> ActiveDirectory { get; set; }
    
    public ICollection<AuditLogModel> AuditLog { get; set; }
    
    public Guid UserSessionId { get; set; }
    
    [ForeignKey(nameof(UserSessionId))]
    public UserSessionModel UserSession { get; set; }
    
    //public Guid? HumanResourceId { get; set; }
    
    //[ForeignKey(nameof(HumanResourceId))]
    //public HumanResourceModel? HumanResource { get; set; }
    
    public required PermissionEnum Permission { get; set; }
    
    public required StatusEnum Status { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public required DateTime UpdatedAt { get; set; }
}