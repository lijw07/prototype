using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class HumanResourceModel
{
    [Key]
    public required Guid HumanResourceId { get; set; }
    
    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }
    
    [Required]
    public required Guid EmployeeNumber { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string Location { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}