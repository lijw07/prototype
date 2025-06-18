using System.ComponentModel.DataAnnotations;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    public required string ApplicationName { get; set; }
    
    public string? ApplicationDescription { get; set; }
    
    [Required]
    public required DataSourceTypeEnum ApplicationDataSourceType { get; set; }
    
    [Required]
    public required ApplicationConnectionModel ApplicationConnections { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}