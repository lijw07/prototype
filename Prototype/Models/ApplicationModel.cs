using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    public required string ApplicationName { get; set; }
    
    public string ApplicationDescription { get; set; }
    
    public required DataSourceTypeEnum ApplicationDataSourceType { get; set; }
    
    public ApplicationConnectionModel ApplicationConnections { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}