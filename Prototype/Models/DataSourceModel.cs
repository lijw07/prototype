using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class DataSourceModel
{
    [Key]
    public required Guid DataSourceId { get; set; }
    
    [Required]
    public required string DataSourceName { get; set; }
    
    
    public string Description { get; set; }
    
    [Required]
    public required string Host { get; set; }
    
    [Required]
    public required string Port { get; set; }
    
    [Required]
    public required DataSourceTypeEnum DataSourceType { get; set; }
    
    public AuthenticationModel Authentication { get; set; }
    
    public string DatabaseName { get; set; }
}