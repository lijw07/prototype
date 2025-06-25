using Microsoft.Data.SqlClient;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Cloud;

public class AzureSqlDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<AzureSqlDatabaseStrategy> logger)
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
            { AuthenticationTypeEnum.AzureAdInteractive, true },
            { AuthenticationTypeEnum.AzureAdDefault, true },
            { AuthenticationTypeEnum.AzureAdMsi, true },
            { AuthenticationTypeEnum.NoAuth, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = BuildAzureDataSource(source.Host, source.Port),
            InitialCatalog = source.DatabaseName ?? "master",
            TrustServerCertificate = false, // Azure SQL requires SSL
            Encrypt = true, // Always encrypt for Azure SQL
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

            case AuthenticationTypeEnum.AzureAdInteractive:
                if (!string.IsNullOrEmpty(source.Username))
                    builder.UserID = source.Username;
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                break;

            case AuthenticationTypeEnum.AzureAdDefault:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                break;

            case AuthenticationTypeEnum.AzureAdMsi:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;
                if (!string.IsNullOrEmpty(source.Username))
                    builder.UserID = source.Username; // User-assigned managed identity
                break;

            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for Azure SQL Database.");
        }

        return builder.ConnectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = BuildAzureDataSource(source.Host, source.Port),
            InitialCatalog = source.DatabaseName ?? "master",
            TrustServerCertificate = false, // Azure SQL requires SSL
            Encrypt = true, // Always encrypt for Azure SQL
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

            case AuthenticationTypeEnum.AzureAdInteractive:
                if (!string.IsNullOrEmpty(source.Username))
                    builder.UserID = source.Username;
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive;
                break;

            case AuthenticationTypeEnum.AzureAdDefault:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;
                break;

            case AuthenticationTypeEnum.AzureAdMsi:
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;
                if (!string.IsNullOrEmpty(source.Username))
                    builder.UserID = source.Username; // User-assigned managed identity
                break;

            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for Azure SQL Database.");
        }

        return builder.ConnectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        logger.LogInformation("Azure SQL: Testing connection...");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            var success = result != null && result.ToString() == "1";
            logger.LogInformation("Azure SQL: Test query result: {Result}, success: {Success}", result, success);
            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Azure SQL Database connection test failed: {Error}", ex.Message);
            return false;
        }
    }

    private string BuildAzureDataSource(string host, string port)
    {
        // Azure SQL Database format: servername.database.windows.net
        if (host.Contains(".database.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            // Already in Azure SQL format, use as-is (port is typically 1433 and not needed)
            return host;
        }

        // If it's just a server name, append the Azure SQL suffix
        var serverName = host;
        if (!string.IsNullOrEmpty(port) && port != "1433")
        {
            return $"{serverName}.database.windows.net,{port}";
        }
        
        return $"{serverName}.database.windows.net";
    }
}