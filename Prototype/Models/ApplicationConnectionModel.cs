using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationConnectionModel
{
    [Key]
    public required Guid ApplicationConnectionId { get; set; }
    
    [Required]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public ApplicationModel Application { get; set; }
    
    [Required]
    public required string Instance { get; set; }
    
    [Required]
    public required string Host { get; set; }
    
    [Required]
    public required string Port { get; set; }
    
    [Required]
    public required AuthenticationTypeEnum AuthenticationType { get; set; }
    
    public string DatabaseName { get; set; }
    
    public required string Url { get; set; }
    
    public string Username { get; set; }
    
    public string Password { get; set; }
    
    public string AuthenticationDatabase { get; set; }
    
    public string AwsAccessKeyId { get; set; }
    
    public string AwsSecretAccessKey { get; set; }
    
    public string AwsSessionToken { get; set; }
    
    public string Principal { get; set; }
    
    public string ServiceName { get; set; }
    
    public string ServiceRealm { get; set; }
    
    public bool CanonicalizeHostName { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}