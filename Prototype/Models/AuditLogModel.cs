using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Models;

public class AuditLogModel
{
    [Key]
    public required Guid AuditLogId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }
    
    [Required]
    public ActionTypeEnum ActionType { get; set; }
    
    [Required]
    public required string Description { get; set; }
    
    [Required]
    public string Metadata { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
}