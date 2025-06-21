using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class UserRecoveryRequestModel
{
    [Key]
    public required Guid UserRecoveryRequestId { get; set; }

    [Required]
    public required Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public required UserModel User { get; set; }

    [Required]
    public required string Token { get; set; }
    
    [Required]
    public required bool IsUsed { get; set; } = false;
    
    [Required]
    public required UserRecoveryTypeEnum RecoveryType { get; set; }

    [Required]
    public required DateTime RequestedAt { get; set; }

    [Required]
    public required DateTime ExpiresAt { get; set; }
}