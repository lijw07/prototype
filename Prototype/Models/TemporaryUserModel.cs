using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class TemporaryUserModel
{
    [Key]
    public required Guid TemporaryUserId { get; set; }

    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string PasswordHash { get; set; }
    
    [Required]
    public required string Firstname { get; set; }
    
    [Required]
    public required string Lastname { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    public required string Manager { get; set; }
    
    public required string Department { get; set; }
    
    [Required]
    public required string Location { get; set; }
    
    public required string JobTitle { get; set; }
    
    public required ICollection<ApplicationModel> Application { get; set; }
    
    public required ICollection<ActiveDirectoryModel> ActiveDirectory { get; set; }
    
    public required ICollection<AuditLogModel> AuditLog { get; set; }
    
    public required Guid UserSessionId { get; set; }
    
    [ForeignKey(nameof(UserSessionId))]
    public required UserSessionModel UserSession { get; set; }
    
    //public required Guid HumanResourceId { get; set; }
    
    //[ForeignKey(nameof(HumanResourceId))]
    //public required HumanResourceModel HumanResource { get; set; }
    
    public required PermissionEnum Permission { get; set; }
    
    public required StatusEnum Status { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public required DateTime UpdatedAt { get; set; }

    [Required]
    public required string VerificationCode { get; set; }
    
    [Required]
    public required DateTime RequestedAt { get; set; }
}