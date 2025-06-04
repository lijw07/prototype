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
    
    public required ICollection<UserModel> User { get; set; }

    [Required]
    public required string ResourceAffected { get; set; }
    
    [Required]
    public required DateTime Timestamp { get; set; }
}