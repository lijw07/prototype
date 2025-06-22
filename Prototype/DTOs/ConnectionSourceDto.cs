using System.ComponentModel.DataAnnotations;
using Prototype.Enum;

namespace Prototype.DTOs;

public class ConnectionSourceDto
{
    [Required(ErrorMessage = "Host is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Host must be between 1 and 255 characters")]
    public required string Host { get; set; }

    [Required(ErrorMessage = "Port is required")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Port must be a valid number")]
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public required string Port { get; set; }

    [StringLength(100, ErrorMessage = "Instance name cannot exceed 100 characters")]
    public string? Instance { get; set; }

    [Required(ErrorMessage = "Authentication type is required")]
    public required AuthenticationTypeEnum AuthenticationType { get; set; }

    [StringLength(100, ErrorMessage = "Database name cannot exceed 100 characters")]
    public string? DatabaseName { get; set; }

    [Required(ErrorMessage = "Connection string is required")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Connection string must be between 1 and 500 characters")]
    public required string Url { get; set; }

    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    public string? Username { get; set; }

    [StringLength(255, ErrorMessage = "Password cannot exceed 255 characters")]
    public string? Password { get; set; }

    [StringLength(100, ErrorMessage = "Authentication database cannot exceed 100 characters")]
    public string? AuthenticationDatabase { get; set; }

    [StringLength(255, ErrorMessage = "AWS Access Key ID cannot exceed 255 characters")]
    public string? AwsAccessKeyId { get; set; }

    [StringLength(255, ErrorMessage = "AWS Secret Access Key cannot exceed 255 characters")]
    public string? AwsSecretAccessKey { get; set; }

    [StringLength(255, ErrorMessage = "AWS Session Token cannot exceed 255 characters")]
    public string? AwsSessionToken { get; set; }

    [StringLength(255, ErrorMessage = "Principal cannot exceed 255 characters")]
    public string? Principal { get; set; }

    [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters")]
    public string? ServiceName { get; set; }

    [StringLength(100, ErrorMessage = "Service realm cannot exceed 100 characters")]
    public string? ServiceRealm { get; set; }

    public bool CanonicalizeHostName { get; set; }
    
    // API-specific properties
    [StringLength(1000, ErrorMessage = "API endpoint cannot exceed 1000 characters")]
    public string? ApiEndpoint { get; set; }
    
    [StringLength(20, ErrorMessage = "HTTP method cannot exceed 20 characters")]
    public string? HttpMethod { get; set; }
    
    [StringLength(2000, ErrorMessage = "Headers cannot exceed 2000 characters")]
    public string? Headers { get; set; }
    
    [StringLength(5000, ErrorMessage = "Request body cannot exceed 5000 characters")]
    public string? RequestBody { get; set; }
    
    [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
    public string? ApiKey { get; set; }
    
    [StringLength(1000, ErrorMessage = "Bearer token cannot exceed 1000 characters")]
    public string? BearerToken { get; set; }
    
    [StringLength(500, ErrorMessage = "Client ID cannot exceed 500 characters")]
    public string? ClientId { get; set; }
    
    [StringLength(1000, ErrorMessage = "Client secret cannot exceed 1000 characters")]
    public string? ClientSecret { get; set; }
    
    [StringLength(1000, ErrorMessage = "Refresh token cannot exceed 1000 characters")]
    public string? RefreshToken { get; set; }
    
    [StringLength(1000, ErrorMessage = "Authorization URL cannot exceed 1000 characters")]
    public string? AuthorizationUrl { get; set; }
    
    [StringLength(1000, ErrorMessage = "Token URL cannot exceed 1000 characters")]
    public string? TokenUrl { get; set; }
    
    [StringLength(500, ErrorMessage = "Scope cannot exceed 500 characters")]
    public string? Scope { get; set; }
    
    // File-specific properties
    [StringLength(1000, ErrorMessage = "File path cannot exceed 1000 characters")]
    public string? FilePath { get; set; }
    
    [StringLength(100, ErrorMessage = "File format cannot exceed 100 characters")]
    public string? FileFormat { get; set; }
    
    [StringLength(10, ErrorMessage = "Delimiter cannot exceed 10 characters")]
    public string? Delimiter { get; set; }
    
    [StringLength(10, ErrorMessage = "Encoding cannot exceed 10 characters")]
    public string? Encoding { get; set; }
    
    public bool HasHeader { get; set; }
    
    [StringLength(2000, ErrorMessage = "Custom properties cannot exceed 2000 characters")]
    public string? CustomProperties { get; set; }
}