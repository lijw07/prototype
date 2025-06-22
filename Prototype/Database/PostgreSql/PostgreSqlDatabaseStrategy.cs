using Npgsql;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.PostgreSql;

public class PostgreSqlDatabaseStrategy : IDatabaseConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;
    private readonly ILogger<PostgreSqlDatabaseStrategy> _logger;

    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.PostgreSql;

    public PostgreSqlDatabaseStrategy(
        PasswordEncryptionService encryptionService,
        ILogger<PostgreSqlDatabaseStrategy> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true },
            { AuthenticationTypeEnum.NoAuth, false },
            { AuthenticationTypeEnum.WindowsIntegrated, true }, // SSPI on Windows
            { AuthenticationTypeEnum.AzureAdPassword, false },
            { AuthenticationTypeEnum.AzureAdIntegrated, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = source.Host,
            Port = int.Parse(source.Port),
            Database = source.DatabaseName
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                builder.Username = source.Username;
                builder.Password = source.Password;
                break;
                
            case AuthenticationTypeEnum.WindowsIntegrated:
                // PostgreSQL Windows authentication (SSPI)
                // This requires proper PostgreSQL configuration for Windows auth
                throw new NotSupportedException("Windows Integrated authentication requires special PostgreSQL configuration and is not currently supported.");
                // break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for PostgreSQL.");
        }

        // Additional PostgreSQL-specific options
        builder.Pooling = true;
        builder.MinPoolSize = 1;
        builder.MaxPoolSize = 100;
        builder.Timeout = 30;
        builder.CommandTimeout = 30;
        // TrustServerCertificate is deprecated in newer Npgsql versions

        return builder.ConnectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = source.Host,
            Port = int.Parse(source.Port),
            Database = source.DatabaseName
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                builder.Username = source.Username;
                builder.Password = string.IsNullOrEmpty(source.Password) ? null : _encryptionService.Decrypt(source.Password);
                break;
                
            case AuthenticationTypeEnum.WindowsIntegrated:
                // PostgreSQL Windows authentication (SSPI)
                // This requires proper PostgreSQL configuration for Windows auth
                throw new NotSupportedException("Windows Integrated authentication requires special PostgreSQL configuration and is not currently supported.");
                // break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for PostgreSQL.");
        }

        builder.Pooling = true;
        builder.MinPoolSize = 1;
        builder.MaxPoolSize = 100;
        builder.Timeout = 30;
        builder.CommandTimeout = 30;
        // TrustServerCertificate is deprecated in newer Npgsql versions

        return builder.ConnectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new NpgsqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null && result.ToString() == "1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PostgreSQL connection test failed");
            return false;
        }
    }
}