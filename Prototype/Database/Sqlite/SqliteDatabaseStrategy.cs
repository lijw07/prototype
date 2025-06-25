using System.Data.Odbc;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Sqlite;

public class SqliteDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<SqliteDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.Sqlite;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true }, // For encrypted databases
            { AuthenticationTypeEnum.NoAuth, true }, // Default for SQLite
            { AuthenticationTypeEnum.WindowsIntegrated, false },
            { AuthenticationTypeEnum.AzureAdPassword, false },
            { AuthenticationTypeEnum.AzureAdIntegrated, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        // SQLite uses file path instead of host/port/database
        string filePath;
        if (!string.IsNullOrEmpty(source.DatabaseName))
        {
            filePath = source.DatabaseName;
        }
        else if (!string.IsNullOrEmpty(source.Host))
        {
            // If host is provided, treat it as file path
            filePath = source.Host;
        }
        else
        {
            throw new ArgumentException("SQLite requires a database file path in either DatabaseName or Host field");
        }

        var connectionString = $"DRIVER={{SQLite3 ODBC Driver}};Database={filePath};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                // SQLite with password (encryption) - not standard in ODBC
                if (!string.IsNullOrEmpty(source.Password))
                {
                    connectionString += $"PWD={source.Password};";
                }
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // Standard SQLite without password
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for SQLite.");
        }

        return connectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        // SQLite uses file path instead of host/port/database
        string filePath;
        if (!string.IsNullOrEmpty(source.DatabaseName))
        {
            filePath = source.DatabaseName;
        }
        else if (!string.IsNullOrEmpty(source.Host))
        {
            filePath = source.Host;
        }
        else
        {
            throw new ArgumentException("SQLite requires a database file path in either DatabaseName or Host field");
        }

        var connectionString = $"DRIVER={{SQLite3 ODBC Driver}};Database={filePath};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                // SQLite with password (encryption) - not standard in ODBC
                if (!string.IsNullOrEmpty(source.Password))
                {
                    var password = encryptionService.Decrypt(source.Password);
                    connectionString += $"PWD={password};";
                }
                break;
                
            case AuthenticationTypeEnum.NoAuth:
                // Standard SQLite without password
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for SQLite.");
        }

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
            logger.LogError(ex, "SQLite ODBC connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}