using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class AuditLogModel
{
    [Key]
    public required Guid AuditLogId { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserModel? User { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    [Required]
    public required string Metadata { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
}