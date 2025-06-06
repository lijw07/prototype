using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Models;

public class UserRecoveryRequestModel
{
    [Key]
    public required Guid UserRecoveryRequestId { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
    
    [Required]
    public required string Token { get; set; }
    
    [Required]
    public UserRecoveryTypeEnum UserRecoveryType { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime ExpiresAt { get; set; }
}