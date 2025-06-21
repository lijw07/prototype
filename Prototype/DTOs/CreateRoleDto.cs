using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs;

public class CreateRoleDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string RoleName { get; set; }
}