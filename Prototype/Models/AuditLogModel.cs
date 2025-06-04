using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class AuditLogModel
{
    [Key]
    public Guid AuditLogId { get; set; }
    
    [Required]
    public ActionTypeEnum ActionType { get; set; }
    
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))] 
    public UserModel User { get; set; }

    [Required]
    public string ResourceAffected { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
}