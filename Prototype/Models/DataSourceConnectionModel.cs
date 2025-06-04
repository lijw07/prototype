using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class DataSourceConnectionModel
{
    [Key]
    public required Guid DataSourceConnectionId { get; set; }
    
    [Required]
    public required string DataSourceName { get; set; }
    
    [Required]
    public required DataSourceTypeEnum DataSourceType { get; set; }
    
    [Required]
    public required Guid ConnectionCredentialId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ConnectionCredentialId))]
    public required ConnectionCredentialModel ConnectionCredential { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}