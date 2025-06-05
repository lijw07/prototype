using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class UserActivityLogModel
{
    [Key]
    public required Guid UserActivityLogId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
    
    [Required]
    public required string IPAddress { get; set; }
    
    [Required]
    public required string DeviceInformation { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    [Required]
    public required string Description { get; set; }
    
    [Required]
    public required DateTime Timestamp { get; set; }
}