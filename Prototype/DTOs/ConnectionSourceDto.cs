using Prototype.Enum;

namespace Prototype.DTOs;

public class ConnectionSourceDto
{
    public required string Host { get; set; }
    public required string Port { get; set; }
    public string? Instance { get; set; }
    public required AuthenticationTypeEnum AuthenticationType { get; set; }
    public string? DatabaseName { get; set; }
    public required string Url { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? AuthenticationDatabase { get; set; }
    public string? AwsAccessKeyId { get; set; }
    public string? AwsSecretAccessKey { get; set; }
    public string? AwsSessionToken { get; set; }
    public string? Principal { get; set; }
    public string? ServiceName { get; set; }
    public string? ServiceRealm { get; set; }
    public bool CanonicalizeHostName { get; set; }
}