using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class AuthenticationModel
{
    [Key]
    public required Guid AuthenticationId { get; set; }
    
    [Required]
    public required AuthenticationTypeEnum Authentication { get; set; }
    
    [Required]
    public required Guid DataSourceId { get; set; }
    
    [Required]
    [ForeignKey(nameof(DataSourceId))]
    public required DataSourceModel DataSource { get; set; }
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    public string? AuthenticationDatabase { get; set; }
    
    public string? AwsAccessKeyId { get; set; }
    
    public string? AwsSecretKey { get; set; }
    
    public string? AwsSessionToken { get; set; }
    
    public string? Principal { get; set; }
    
    public string? ServiceName { get; set; }
    
    public string? ServiceRealm { get; set; }
    
    public bool? CanonicalizeHostName { get; set; }
}