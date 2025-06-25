using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class UserRequestModel
{
    [Key]
    public Guid UserRequestId { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(50)]
    public required string ToolId { get; set; }
    
    [Required]
    [StringLength(255)]
    public required string ToolName { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string ToolCategory { get; set; }
    
    [Required]
    [StringLength(2000)]
    public required string Reason { get; set; }
    
    [Required]
    public UserRequestStatusEnum Status { get; set; } = UserRequestStatusEnum.Pending;
    
    [Required]
    public RequestPriorityEnum Priority { get; set; } = RequestPriorityEnum.Medium;
    
    [Required]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReviewedAt { get; set; }
    
    [StringLength(255)]
    public string? ReviewedBy { get; set; }
    
    [StringLength(1000)]
    public string? Comments { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual UserModel User { get; set; } = null!;
}