using System.Data.Odbc;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Database.MySql;

public class MySqlDatabaseStrategy(
    IPasswordEncryptionService encryptionService,
    ILogger<MySqlDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MySql;

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
        var connectionString = $"DRIVER={{MySQL ODBC 8.0 Unicode Driver}};SERVER={source.Host};PORT={source.Port};DATABASE={source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                connectionString += $"UID={source.Username};PWD={source.Password};";
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MySQL typically still needs credentials, but we can try without
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MySQL.");
        }

        // Additional MySQL ODBC options
        connectionString += "CHARSET=utf8mb4;SSLMODE=PREFERRED;";

        return connectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var connectionString = $"DRIVER={{MySQL ODBC 8.0 Unicode Driver}};SERVER={source.Host};PORT={source.Port};DATABASE={source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                var password = string.IsNullOrEmpty(source.Password) ? "" : encryptionService.Decrypt(source.Password);
                connectionString += $"UID={source.Username};PWD={password};";
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MySQL typically still needs credentials
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MySQL.");
        }

        connectionString += "CHARSET=utf8mb4;SSLMODE=PREFERRED;";

        return connectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new OdbcConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new OdbcCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null && result.ToString() == "1";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MySQL ODBC connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}