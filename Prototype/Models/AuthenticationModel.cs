using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Models;

public class AuthenticationModel
{
    [Key]
    public required Guid AuthenticationId { get; set; }
    
    [Required]
    public AuthenticationTypeEnum Authentication { get; set; }
    
    [Required]
    public required Guid DataSourceId { get; set; }
    
    [Required]
    [ForeignKey(nameof(DataSourceId))]
    public DataSourceModel DataSource { get; set; }
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    public string? AuthenticationDatabase { get; set; }
    
    public string? AWSAccessKeyId { get; set; }
    
    public string? AWSSecretKey { get; set; }
    
    public string? AWSSessionToken { get; set; }
    
    public string? Principal { get; set; }
    
    public string? ServiceName { get; set; }
    
    public string? ServiceRealm { get; set; }
    
    public bool? CanonicalizeHostName { get; set; }
}