using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class UserSessionModel
{
    [Key]
    public required Guid UserSessionId { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    [Required]
    public required string ResourceAffected { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
}