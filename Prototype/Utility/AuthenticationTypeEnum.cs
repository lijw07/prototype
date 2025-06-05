namespace Prototype.Utility;

public enum AuthenticationTypeEnum
{
    UserPassword,
    Kerberos,
    AzureAdPassword,
    AzureAdInteractive,
    AzureAdIntegrated,
    AzureAdDefault,
    AzureAdMsi,
    ScramSha1,
    ScramSha256,
    AwsIam,
    X509,
    GssapiKerberos,
    PlainLdap,
    NoAuth
}