using Microsoft.Data.SqlClient;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Database.MicrosoftSQLServer;

public class SqlServerDatabaseStrategy(
    IPasswordEncryptionService encryptionService,
    ILogger<SqlServerDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MicrosoftSqlServer;

    public Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes()
    {
        return new Dictionary<AuthenticationTypeEnum, bool>
        {
            { AuthenticationTypeEnum.UserPassword, true },
            { AuthenticationTypeEnum.AzureAdPassword, true },
            { AuthenticationTypeEnum.AzureAdIntegrated, true },
            { AuthenticationTypeEnum.WindowsIntegrated, true },
            { AuthenticationTypeEnum.NoAuth, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = IsLocalDbInstance(source.Host) ? source.Host : $"{source.Host},{source.Port}",
            InitialCatalog = source.DatabaseName ?? "master",
            TrustServerCertificate = true,
            ConnectTimeout = 30,
            CommandTimeout = 30
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                if (string.IsNullOrEmpty(source.Username) || string.IsNullOrEmpty(source.Password))
                    throw new ArgumentException("Username and password are required for UserPassword authentication.");
                builder.UserID = source.Username;
                builder.Password = source.Password;
                break;

            case AuthenticationTypeEnum.WindowsIntegrated:
                builder.IntegratedSecurity = true;
                break;

            case AuthenticationTypeEnum.AzureAdPassword:
                if (string.IsNullOrEmpty(source.Username) || string.IsNullOrEmpty(source.Password))
                    throw new ArgumentException("Username and password are required for Azure AD Password authentication.");
                builder.UserID = source.Username;
                builder.Password = source.Password;
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                break;

            case AuthenticationTypeEnum.AzureAdIntegrated:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                break;

            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for SQL Server.");
        }

        return builder.ConnectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = IsLocalDbInstance(source.Host) ? source.Host : $"{source.Host},{source.Port}",
            InitialCatalog = source.DatabaseName ?? "master",
            TrustServerCertificate = true,
            ConnectTimeout = 30,
            CommandTimeout = 30
        };

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                if (string.IsNullOrEmpty(source.Username) || string.IsNullOrEmpty(source.Password))
                    throw new ArgumentException("Username and password are required for UserPassword authentication.");
                builder.UserID = source.Username;
                builder.Password = encryptionService.Decrypt(source.Password);
                break;

            case AuthenticationTypeEnum.WindowsIntegrated:
                builder.IntegratedSecurity = true;
                break;

            case AuthenticationTypeEnum.AzureAdPassword:
                if (string.IsNullOrEmpty(source.Username) || string.IsNullOrEmpty(source.Password))
                    throw new ArgumentException("Username and password are required for Azure AD Password authentication.");
                builder.UserID = source.Username;
                builder.Password = encryptionService.Decrypt(source.Password);
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryPassword;
                break;

            case AuthenticationTypeEnum.AzureAdIntegrated:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryIntegrated;
                break;

            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for SQL Server.");
        }

        return builder.ConnectionString;
    }

    private bool IsLocalDbInstance(string host)
    {
        if (string.IsNullOrEmpty(host))
            return false;
            
        // LocalDB instance patterns
        return host.StartsWith("(localdb)\\", StringComparison.OrdinalIgnoreCase) ||
               host.StartsWith("(LocalDB)\\", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("(localdb)", StringComparison.OrdinalIgnoreCase) ||
               host.Equals("(LocalDB)", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        logger.LogInformation("SQL Server: Testing connection with connection string: {ConnectionString}", 
            connectionString.Substring(0, Math.Min(100, connectionString.Length)) + "...");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            logger.LogInformation("SQL Server: Opening connection...");
            await connection.OpenAsync();
            
            logger.LogInformation("SQL Server: Connection opened successfully, executing test query...");
            using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            var success = result != null && result.ToString() == "1";
            logger.LogInformation("SQL Server: Test query result: {Result}, success: {Success}", result, success);
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SQL Server connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}