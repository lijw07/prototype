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

    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
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
}