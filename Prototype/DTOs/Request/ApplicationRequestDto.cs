using System.ComponentModel.DataAnnotations;
using Prototype.DTOs.Request;
using Prototype.Enum;

namespace Prototype.DTOs;

public class ApplicationRequestDto
{
    [Required(ErrorMessage = "Application name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Application name must be between 1 and 100 characters")]
    public required string ApplicationName { get; set; }

    [StringLength(500, ErrorMessage = "Application description cannot exceed 500 characters")]
    public string? ApplicationDescription { get; set; }

    [Required(ErrorMessage = "Data source type is required")]
    public required DataSourceTypeEnum DataSourceType { get; set; }

    [Required(ErrorMessage = "Connection source is required")]
    public required ConnectionSourceRequestDto ConnectionSourceRequest { get; set; }
}