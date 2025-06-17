using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class UserRecoveryRequestModel
{
    [Key]
    public Guid UserRecoveryRequestId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public required UserModel User { get; set; }

    [Required]
    public required string VerificationCode { get; set; }
    
    [Required]
    public bool IsUsed { get; set; } = false;
    
    [Required]
    public UserRecoveryTypeEnum RecoveryType { get; set; }

    [Required]
    public DateTime RequestedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }
}