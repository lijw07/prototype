using System.Data.Odbc;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.MariaDb;

public class MariaDbDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<MariaDbDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MariaDb;

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

    public string BuildConnectionString(ConnectionSourceRequestDto sourceRequest)
    {
        var connectionString = $"DRIVER={{MariaDB ODBC 3.1 Driver}};SERVER={sourceRequest.Host};PORT={sourceRequest.Port};DATABASE={sourceRequest.DatabaseName};";

        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                connectionString += $"UID={sourceRequest.Username};PWD={sourceRequest.Password};";
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MariaDB typically still needs credentials, but we can try without
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for MariaDB.");
        }

        // MariaDB ODBC options
        connectionString += "CHARSET=utf8mb4;AUTO_RECONNECT=1;";

        return connectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var connectionString = $"DRIVER={{MariaDB ODBC 3.1 Driver}};SERVER={source.Host};PORT={source.Port};DATABASE={source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                var password = string.IsNullOrEmpty(source.Password) ? "" : encryptionService.Decrypt(source.Password);
                connectionString += $"UID={source.Username};PWD={password};";
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // MariaDB typically still needs credentials
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for MariaDB.");
        }

        connectionString += "CHARSET=utf8mb4;AUTO_RECONNECT=1;";

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
            logger.LogError(ex, "MariaDB ODBC connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}