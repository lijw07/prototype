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
    public ApplicationModel? Application { get; set; }
    
    [StringLength(100)]
    public string? Instance { get; set; }
    
    [Required]
    [StringLength(255)]
    public required string Host { get; set; }
    
    [Required]
    [StringLength(10)]
    public required string Port { get; set; }
    
    [Required]
    public required AuthenticationTypeEnum AuthenticationType { get; set; }
    
    [StringLength(100)]
    public string? DatabaseName { get; set; }
    
    [Required]
    [StringLength(500)]
    public required string Url { get; set; }
    
    [StringLength(100)]
    public string? Username { get; set; }
    
    // Encrypted password storage
    [StringLength(500)]
    public string? Password { get; set; }
    
    [StringLength(100)]
    public string? AuthenticationDatabase { get; set; }
    
    [StringLength(255)]
    public string? AwsAccessKeyId { get; set; }
    
    [StringLength(500)]
    public string? AwsSecretAccessKey { get; set; }
    
    [StringLength(500)]
    public string? AwsSessionToken { get; set; }
    
    [StringLength(255)]
    public string? Principal { get; set; }
    
    [StringLength(100)]
    public string? ServiceName { get; set; }
    
    [StringLength(100)]
    public string? ServiceRealm { get; set; }
    
    public bool CanonicalizeHostName { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
    
    // API-specific fields
    [StringLength(1000)]
    public string? ApiEndpoint { get; set; }
    
    [StringLength(20)]
    public string? HttpMethod { get; set; }
    
    [StringLength(2000)]
    public string? Headers { get; set; } // JSON string
    
    [StringLength(5000)]
    public string? RequestBody { get; set; }
    
    [StringLength(500)]
    public string? ApiKey { get; set; }
    
    [StringLength(1000)]
    public string? BearerToken { get; set; }
    
    [StringLength(500)]
    public string? ClientId { get; set; }
    
    [StringLength(1000)]
    public string? ClientSecret { get; set; }
    
    [StringLength(1000)]
    public string? RefreshToken { get; set; }
    
    [StringLength(1000)]
    public string? AuthorizationUrl { get; set; }
    
    [StringLength(1000)]
    public string? TokenUrl { get; set; }
    
    [StringLength(500)]
    public string? Scope { get; set; }
    
    // File-specific fields
    [StringLength(1000)]
    public string? FilePath { get; set; }
    
    [StringLength(100)]
    public string? FileFormat { get; set; }
    
    [StringLength(10)]
    public string? Delimiter { get; set; }
    
    [StringLength(10)]
    public string? Encoding { get; set; }
    
    public bool HasHeader { get; set; }
    
    [StringLength(2000)]
    public string? CustomProperties { get; set; } // JSON string for extensibility

    // Navigation properties
    public virtual ICollection<UserApplicationModel> UserApplications { get; set; } = new List<UserApplicationModel>();
}