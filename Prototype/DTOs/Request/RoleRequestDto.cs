using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs.Request;

public class RoleRequestDto
{
    [Required]
    public Guid UserRoleId { get; set; }
    
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string RoleName { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public string CreatedBy { get; set; } = string.Empty;
}