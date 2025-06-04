using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class ActiveDirectoryModel
{
    [Key]
    public required Guid ActiveDirectoryId { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string Username { get; set; }
    
    [Required]
    public required string Password { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }
}