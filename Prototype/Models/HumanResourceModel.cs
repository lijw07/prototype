using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class HumanResourceModel
{
    [Key]
    public Guid HumanResourceId { get; set; }
    
    [Required]
    public string Firstname { get; set; }
    
    [Required]
    public string Lastname { get; set; }
    
    [Required]
    public string Email { get; set; }
    
    [Required]
    public string PhoneNumber { get; set; }
    
    [Required]
    public string Manager { get; set; }
    
    [Required]
    public string Department { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public PermissionEnum Permission { get; set; }
    
    public ICollection<UserModel> User { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}