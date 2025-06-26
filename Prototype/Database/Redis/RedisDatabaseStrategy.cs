using StackExchange.Redis;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Redis;

public class RedisDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<RedisDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.Redis;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true }, // Redis ACL
            { AuthenticationTypeEnum.NoAuth, true }
        };
    }

    public string BuildConnectionString(ConnectionSourceRequestDto sourceRequest)
    {
        var config = new ConfigurationOptions
        {
            EndPoints = { { sourceRequest.Host, int.Parse(sourceRequest.Port) } },
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            ConnectRetry = 3
        };

        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                if (!string.IsNullOrEmpty(sourceRequest.Username) && sourceRequest.Username != "default")
                {
                    // Redis ACL (Redis 6.0+)
                    config.User = sourceRequest.Username;
                    config.Password = sourceRequest.Password;
                }
                else
                {
                    // Legacy AUTH
                    config.Password = sourceRequest.Password;
                }
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // No authentication
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for Redis.");
        }

        // Database selection (0-15)
        if (!string.IsNullOrEmpty(sourceRequest.DatabaseName) && int.TryParse(sourceRequest.DatabaseName, out var dbNumber))
        {
            config.DefaultDatabase = dbNumber;
        }

        // SSL/TLS
        if (sourceRequest.Host != "localhost" && sourceRequest.Host != "127.0.0.1")
        {
            config.Ssl = true;
            config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
        }

        return config.ToString();
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var dto = new ConnectionSourceRequestDto
        {
            Host = source.Host,
            Port = source.Port,
            DatabaseName = source.DatabaseName,
            AuthenticationType = source.AuthenticationType,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            Url = source.Url ?? string.Empty
        };

        return BuildConnectionString(dto);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var db = redis.GetDatabase();
            
            // Ping test
            var pong = await db.PingAsync();
            
            return redis.IsConnected && pong.TotalMilliseconds > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis connection test failed");
            return false;
        }
    }
}