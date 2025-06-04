using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class HumanResourceModel
{
    [Key]
    public required Guid HumanResourceId { get; set; }
    
    [Required]
    public required string Firstname { get; set; }
    
    [Required]
    public required string Lastname { get; set; }
    
    [Required]
    public required string Email { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    [Required]
    public required string Manager { get; set; }
    
    [Required]
    public required string Department { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required PermissionEnum Permission { get; set; }
    
    public required ICollection<UserModel> User { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}