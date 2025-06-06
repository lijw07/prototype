using Prototype.Enum;

namespace Prototype.Utility;

public class DataSourceMapping
{
    public static readonly Dictionary<DataSourceTypeEnum, AuthenticationTypeEnum[]> DataSourceAuthMap = new()
    {
        [DataSourceTypeEnum.MicrosoftSqlServer] = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.AzureAdInteractive,
            AuthenticationTypeEnum.AzureAdIntegrated,
            AuthenticationTypeEnum.AzureAdDefault,
            AuthenticationTypeEnum.AzureAdMsi,
            AuthenticationTypeEnum.NoAuth
        },
        [DataSourceTypeEnum.MySql] = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.NoAuth
        },
        [DataSourceTypeEnum.MongoDb] = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256,
            AuthenticationTypeEnum.AwsIam,
            AuthenticationTypeEnum.X509,
            AuthenticationTypeEnum.GssapiKerberos,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.NoAuth
        }
    };
    
    public static readonly Dictionary<AuthenticationTypeEnum, string[]> AuthTypeFieldsMap = new()
    {
        [AuthenticationTypeEnum.UserPassword] = new[] { "Username", "Password" },
        [AuthenticationTypeEnum.Kerberos] = new[] { "Username", "Password"},
        [AuthenticationTypeEnum.AzureAdPassword] = new[] { "Username", "Password" },
        [AuthenticationTypeEnum.AzureAdInteractive] = new[] { "Username" },
        [AuthenticationTypeEnum.AzureAdIntegrated] = Array.Empty<string>(),
        [AuthenticationTypeEnum.AzureAdDefault] = Array.Empty<string>(),
        [AuthenticationTypeEnum.AzureAdMsi] = new[] { "Username" },
        [AuthenticationTypeEnum.ScramSha1] = new[] { "Username", "Password", "AuthenticationDatabase" },
        [AuthenticationTypeEnum.ScramSha256] = new[] { "Username", "Password", "AuthenticationDatabase" },
        [AuthenticationTypeEnum.AwsIam] = new[] { "AWSAccessKeyId", "AWSSecretKey", "AWSSessionToken" },
        [AuthenticationTypeEnum.X509] = Array.Empty<string>(),
        [AuthenticationTypeEnum.GssapiKerberos] = new[] { "Principal", "ServiceName", "ServiceRealm", "CanonicalizeHostName" },
        [AuthenticationTypeEnum.PlainLdap] = new[] { "Username", "Password" },
        [AuthenticationTypeEnum.NoAuth] = Array.Empty<string>()
    };
}