namespace Prototype.Enum;

public enum AuthenticationTypeEnum
{
    // Database authentication
    UserPassword,
    Kerberos,
    AzureAdPassword,
    AzureAdInteractive,
    AzureAdIntegrated,
    AzureAdDefault,
    AzureAdMsi,
    WindowsIntegrated,
    ScramSha1,
    ScramSha256,
    AwsIam,
    X509,
    GssApi,
    GssapiKerberos,
    Plain,
    PlainLdap,
    NoAuth,
    
    // API authentication
    ApiKey,
    BearerToken,
    BasicAuth,
    OAuth1,
    OAuth2,
    JwtToken,
    Digest,
    Custom,
    
    // File authentication
    FileSystem,
    SharedAccessSignature,
    AccessKey,
    ServicePrincipal,
    
    // AWS authentication
    AwsAccessKey,
    AwsSessionToken,
    
    // Azure authentication
    AzureStorageKey,
    AzureSas
}