using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class UserModel
{
    [Key]
    public required Guid UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string FirstName { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string LastName { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Username { get; set; }
    
    [Required]
    [StringLength(255)]
    public required string PasswordHash { get; set; }
    
    [Required]
    [StringLength(255)]
    public required string Email { get; set; }
    
    [Required]
    [StringLength(20)]
    public required string PhoneNumber { get; set; }
    
    [Required]
    public required bool IsActive { get; set; } = true;
    
    [Required]
    [StringLength(50)]
    public required string Role { get; set; } = "User";
    
    public DateTime LastLogin { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserActivityLogModel> ActivityLogs { get; set; } = new List<UserActivityLogModel>();
    public virtual ICollection<AuditLogModel> AuditLogs { get; set; } = new List<AuditLogModel>();
    public virtual ICollection<UserRecoveryRequestModel> RecoveryRequests { get; set; } = new List<UserRecoveryRequestModel>();
    public virtual ICollection<UserApplicationModel> UserApplications { get; set; } = new List<UserApplicationModel>();
}