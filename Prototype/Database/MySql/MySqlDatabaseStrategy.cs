using MySql.Data.MySqlClient;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.MySql;

public class MySqlDatabaseStrategy : IDatabaseConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;
    private readonly ILogger<MySqlDatabaseStrategy> _logger;

    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MySql;

    public MySqlDatabaseStrategy(
        PasswordEncryptionService encryptionService,
        ILogger<MySqlDatabaseStrategy> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true },
            { AuthenticationTypeEnum.NoAuth, true },
            { AuthenticationTypeEnum.WindowsIntegrated, false },
            { AuthenticationTypeEnum.AzureAdPassword, false },
            { AuthenticationTypeEnum.AzureAdIntegrated, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = source.Host,
            Port = uint.Parse(source.Port),
            Database = source.DatabaseName
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                builder.UserID = source.Username;
                builder.Password = source.Password;
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MySQL typically still needs credentials, but we can try without
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MySQL.");
        }

        // Additional MySQL-specific options
        builder.SslMode = MySqlSslMode.Preferred;
        builder.AllowPublicKeyRetrieval = true;
        builder.CharacterSet = "utf8mb4";

        return builder.ConnectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = source.Host,
            Port = uint.Parse(source.Port),
            Database = source.DatabaseName
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                builder.UserID = source.Username;
                builder.Password = string.IsNullOrEmpty(source.Password) ? null : _encryptionService.Decrypt(source.Password);
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MySQL typically still needs credentials
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MySQL.");
        }

        builder.SslMode = MySqlSslMode.Preferred;
        builder.AllowPublicKeyRetrieval = true;
        builder.CharacterSet = "utf8mb4";

        return builder.ConnectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new MySqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null && result.ToString() == "1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL connection test failed");
            return false;
        }
    }
}