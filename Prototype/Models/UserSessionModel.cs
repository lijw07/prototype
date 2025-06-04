using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class UserSessionModel
{
    [Key]
    public required Guid UserSessionId { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionTypeEnum { get; set; }
    
    [Required]
    public required string ResourceAffected { get; set; }
    
    [Required]
    public required DateTime Timestamp { get; set; }
}