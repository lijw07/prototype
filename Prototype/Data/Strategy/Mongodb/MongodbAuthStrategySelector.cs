using Prototype.Data.Interface;
using Prototype.Enum;

namespace Prototype.Data.Strategy.Mongodb;

public class MongodbAuthStrategySelector(IServiceProvider provider)
{
    public IMongoAuthStrategy GetStrategy(AuthenticationTypeEnum authType) => authType switch
    {
        AuthenticationTypeEnum.UserPassword or AuthenticationTypeEnum.ScramSha1 =>
            new MongodbScramAuthStrategy("SCRAM-SHA-1"),
        AuthenticationTypeEnum.ScramSha256 =>
            new MongodbScramAuthStrategy("SCRAM-SHA-256"),
        AuthenticationTypeEnum.AwsIam => provider.GetRequiredService<MongodbAwsIamAuthStrategy>(),
        AuthenticationTypeEnum.X509 => provider.GetRequiredService<MongodbX509AuthStrategy>(),
        AuthenticationTypeEnum.Kerberos => provider.GetRequiredService<MongodbKerberosAuthStrategy>(),
        AuthenticationTypeEnum.NoAuth => provider.GetRequiredService<MongodbNoAuthStrategy>(),
        _ => throw new NotSupportedException($"Unsupported MongoDB auth type: {authType}")
    };
}