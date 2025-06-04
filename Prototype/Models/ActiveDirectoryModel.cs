using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class ActiveDirectoryModel
{
    [Key]
    public Guid ActiveDirectoryId { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string Username { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
}