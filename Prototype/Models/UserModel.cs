using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class UserModel
{
    [Key]
    public Guid UserId { get; set; }
    
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
    
    public ICollection<ApplicationModel> Application { get; set; }
    
    public ICollection<ActiveDirectoryModel> ActiveDirectory { get; set; }
    
    public ICollection<AuditLogModel> AuditLog { get; set; }
    
    public Guid UserSessionId { get; set; }
    
    [ForeignKey(nameof(UserSessionId))]
    public UserSessionModel UserSession { get; set; }
    
    [Required]
    public Guid HumanResourceId { get; set; }
    
    [Required]
    [ForeignKey(nameof(HumanResourceId))]
    public HumanResourceModel HumanResource { get; set; }
    
    [Required]
    public PermissionEnum Permission { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}