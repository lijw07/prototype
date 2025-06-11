using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationConnectionModel
{
    [Key]
    public required Guid ApplicationConnectionId { get; set; }
    
    [Required]
    public required string ConnectionName { get; set; }
    
    [Required]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public required ApplicationModel Application { get; set; }
    
    [Required]
    public required Guid DataSourceId { get; set; }
    
    [Required]
    [ForeignKey(nameof(DataSourceId))]
    public required DataSourceModel DataSourceType { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}