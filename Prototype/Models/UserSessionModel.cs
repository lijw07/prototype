using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class UserSessionModel
{
    [Key]
    public Guid UserSessionId { get; set; }
    
    [Required]
    public ActionTypeEnum ActionTypeEnum { get; set; }
    
    [Required]
    public string ResourceAffected { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
}