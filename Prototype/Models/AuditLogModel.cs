using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class AuditLogModel
{
    [Key]
    public required Guid AuditLogId { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    public required Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))] 
    public required UserModel User { get; set; }

    [Required]
    public required string ResourceAffected { get; set; }
    
    [Required]
    public required DateTime Timestamp { get; set; }
}