using MongoDB.Driver;
using MongoDB.Bson;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Database.MongoDb;

public class MongoDbDatabaseStrategy(
    IPasswordEncryptionService encryptionService,
    ILogger<MongoDbDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MongoDb;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true },
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.X509, true },
            { AuthenticationTypeEnum.ScramSha1, true },
            { AuthenticationTypeEnum.ScramSha256, true },
            { AuthenticationTypeEnum.GssApi, true },
            { AuthenticationTypeEnum.Plain, true },
            { AuthenticationTypeEnum.AwsIam, true }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new MongoUrlBuilder();
        
        // Basic connection info
        builder.Server = new MongoServerAddress(source.Host, int.Parse(source.Port));
        
        // Authentication
        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
            case AuthenticationTypeEnum.ScramSha1:
            case AuthenticationTypeEnum.ScramSha256:
                builder.Username = source.Username;
                builder.Password = source.Password;
                builder.AuthenticationSource = source.AuthenticationDatabase ?? "admin";
                
                if (source.AuthenticationType == AuthenticationTypeEnum.ScramSha1)
                    builder.AuthenticationMechanism = "SCRAM-SHA-1";
                else if (source.AuthenticationType == AuthenticationTypeEnum.ScramSha256)
                    builder.AuthenticationMechanism = "SCRAM-SHA-256";
                break;
                
            case AuthenticationTypeEnum.X509:
                builder.AuthenticationMechanism = "MONGODB-X509";
                // Certificate should be configured at the client level
                break;
                
            case AuthenticationTypeEnum.GssApi:
                builder.AuthenticationMechanism = "GSSAPI";
                builder.Username = source.Principal;
                // Note: Authentication mechanism properties require more complex setup
                // This is a simplified implementation
                break;
                
            case AuthenticationTypeEnum.Plain:
                builder.AuthenticationMechanism = "PLAIN";
                builder.Username = source.Username;
                builder.Password = source.Password;
                break;
                
            case AuthenticationTypeEnum.AwsIam:
                builder.AuthenticationMechanism = "MONGODB-AWS";
                builder.Username = source.AwsAccessKeyId;
                builder.Password = source.AwsSecretAccessKey;
                // Note: AWS session token setup requires more complex configuration
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // No authentication
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MongoDB.");
        }

        // Database name
        if (!string.IsNullOrEmpty(source.DatabaseName))
        {
            builder.DatabaseName = source.DatabaseName;
        }

        // Additional options
        builder.ConnectTimeout = TimeSpan.FromSeconds(30);
        builder.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
        builder.UseTls = source.Host != "localhost" && source.Host != "127.0.0.1";

        return builder.ToString();
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var dto = new ConnectionSourceDto
        {
            Host = source.Host,
            Port = source.Port,
            DatabaseName = source.DatabaseName,
            AuthenticationType = source.AuthenticationType,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            AuthenticationDatabase = source.AuthenticationDatabase,
            AwsAccessKeyId = source.AwsAccessKeyId,
            AwsSecretAccessKey = string.IsNullOrEmpty(source.AwsSecretAccessKey) ? null : encryptionService.Decrypt(source.AwsSecretAccessKey),
            AwsSessionToken = string.IsNullOrEmpty(source.AwsSessionToken) ? null : encryptionService.Decrypt(source.AwsSessionToken),
            Principal = source.Principal,
            ServiceName = source.ServiceName,
            ServiceRealm = source.ServiceRealm,
            CanonicalizeHostName = source.CanonicalizeHostName,
            Url = source.Url ?? string.Empty
        };

        return BuildConnectionString(dto);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("admin");
            
            // Ping the database
            var result = await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            
            return result != null && result.Contains("ok") && result["ok"].ToDouble() == 1.0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MongoDB connection test failed");
            return false;
        }
    }
}