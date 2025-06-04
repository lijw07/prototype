using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class DataSourceConnectionModel
{
    [Key]
    public Guid DataSourceConnectionId { get; set; }
    
    [Required]
    public string DataSourceName { get; set; }
    
    [Required]
    public DataSourceTypeEnum DataSourceType { get; set; }
    
    [Required]
    public Guid ConnectionCredentialId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ConnectionCredentialId))]
    public ConnectionCredentialModel ConnectionCredential { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}