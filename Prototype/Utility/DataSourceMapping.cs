using Prototype.Enum;

namespace Prototype.Utility;

public class DataSourceMapping
{
    public static readonly Dictionary<DataSourceTypeEnum, AuthenticationTypeEnum[]> DataSourceAuthMap = new()
    {
        [DataSourceTypeEnum.MicrosoftSqlServer] =
        [
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.AzureAdInteractive,
            AuthenticationTypeEnum.AzureAdIntegrated,
            AuthenticationTypeEnum.AzureAdDefault,
            AuthenticationTypeEnum.AzureAdMsi,
            AuthenticationTypeEnum.NoAuth
        ],
        [DataSourceTypeEnum.MySql] =
        [
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.NoAuth
        ],
        [DataSourceTypeEnum.MongoDb] =
        [
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256,
            AuthenticationTypeEnum.AwsIam,
            AuthenticationTypeEnum.X509,
            AuthenticationTypeEnum.GssapiKerberos,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.NoAuth
        ]
    };
    
    public static readonly Dictionary<AuthenticationTypeEnum, string[]> AuthTypeFieldsMap = new()
    {
        [AuthenticationTypeEnum.UserPassword] = ["Username", "Password"],
        [AuthenticationTypeEnum.Kerberos] = ["Username", "Password"],
        [AuthenticationTypeEnum.AzureAdPassword] = ["Username", "Password"],
        [AuthenticationTypeEnum.AzureAdInteractive] = ["Username"],
        [AuthenticationTypeEnum.AzureAdIntegrated] = [],
        [AuthenticationTypeEnum.AzureAdDefault] = [],
        [AuthenticationTypeEnum.AzureAdMsi] = ["Username"],
        [AuthenticationTypeEnum.ScramSha1] = ["Username", "Password", "AuthenticationDatabase"],
        [AuthenticationTypeEnum.ScramSha256] = ["Username", "Password", "AuthenticationDatabase"],
        [AuthenticationTypeEnum.AwsIam] = ["AWSAccessKeyId", "AWSSecretKey", "AWSSessionToken"],
        [AuthenticationTypeEnum.X509] = [],
        [AuthenticationTypeEnum.GssapiKerberos] = ["Principal", "ServiceName", "ServiceRealm", "CanonicalizeHostName"],
        [AuthenticationTypeEnum.PlainLdap] = ["Username", "Password"],
        [AuthenticationTypeEnum.NoAuth] = []
    };
}