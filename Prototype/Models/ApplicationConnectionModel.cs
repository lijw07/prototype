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

    // Navigation properties
    public virtual ICollection<UserApplicationModel> UserApplications { get; set; } = new List<UserApplicationModel>();
}