using StackExchange.Redis;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Redis;

public class RedisDatabaseStrategy : IDatabaseConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;
    private readonly ILogger<RedisDatabaseStrategy> _logger;

    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.Redis;

    public RedisDatabaseStrategy(
        PasswordEncryptionService encryptionService,
        ILogger<RedisDatabaseStrategy> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true }, // Redis ACL
            { AuthenticationTypeEnum.NoAuth, true }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var config = new ConfigurationOptions
        {
            EndPoints = { { source.Host, int.Parse(source.Port) } },
            AbortOnConnectFail = false,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            ConnectRetry = 3
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                if (!string.IsNullOrEmpty(source.Username) && source.Username != "default")
                {
                    // Redis ACL (Redis 6.0+)
                    config.User = source.Username;
                    config.Password = source.Password;
                }
                else
                {
                    // Legacy AUTH
                    config.Password = source.Password;
                }
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // No authentication
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for Redis.");
        }

        // Database selection (0-15)
        if (!string.IsNullOrEmpty(source.DatabaseName) && int.TryParse(source.DatabaseName, out var dbNumber))
        {
            config.DefaultDatabase = dbNumber;
        }

        // SSL/TLS
        if (source.Host != "localhost" && source.Host != "127.0.0.1")
        {
            config.Ssl = true;
            config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
        }

        return config.ToString();
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
            Password = string.IsNullOrEmpty(source.Password) ? null : _encryptionService.Decrypt(source.Password),
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
            _logger.LogError(ex, "Redis connection test failed");
            return false;
        }
    }
}