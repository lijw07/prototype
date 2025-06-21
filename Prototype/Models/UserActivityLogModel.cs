using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class UserActivityLogModel
{
    [Key]
    public required Guid UserActivityLogId { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserModel? User { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [Required]
    [StringLength(500)]
    public required string DeviceInformation { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    [Required]
    [StringLength(1000)]
    public required string Description { get; set; }
    
    [Required]
    public required DateTime Timestamp { get; set; }
}