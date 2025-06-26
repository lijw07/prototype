using System.Data.Odbc;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.PostgreSql;

public class PostgreSqlDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<PostgreSqlDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.PostgreSql;

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

    public string BuildConnectionString(ConnectionSourceRequestDto sourceRequest)
    {
        var connectionString = $"DRIVER={{PostgreSQL Unicode}};SERVER={sourceRequest.Host};PORT={sourceRequest.Port};DATABASE={sourceRequest.DatabaseName};";

        switch (sourceRequest.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                connectionString += $"UID={sourceRequest.Username};PWD={sourceRequest.Password};";
                break;
                
            case AuthenticationTypeEnum.WindowsIntegrated:
                // PostgreSQL Windows authentication (SSPI)
                throw new NotSupportedException("Windows Integrated authentication requires special PostgreSQL configuration and is not currently supported.");
                
            default:
                throw new NotSupportedException($"Authentication type '{sourceRequest.AuthenticationType}' is not supported for PostgreSQL.");
        }

        // Additional PostgreSQL ODBC options
        connectionString += "SSLMODE=prefer;";

        return connectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var connectionString = $"DRIVER={{PostgreSQL Unicode}};SERVER={source.Host};PORT={source.Port};DATABASE={source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                var password = string.IsNullOrEmpty(source.Password) ? "" : encryptionService.Decrypt(source.Password);
                connectionString += $"UID={source.Username};PWD={password};";
                break;
                
            case AuthenticationTypeEnum.WindowsIntegrated:
                // PostgreSQL Windows authentication (SSPI)
                throw new NotSupportedException("Windows Integrated authentication requires special PostgreSQL configuration and is not currently supported.");
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for PostgreSQL.");
        }

        connectionString += "SSLMODE=prefer;";

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
            logger.LogError(ex, "PostgreSQL ODBC connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}