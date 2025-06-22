using Microsoft.Data.SqlClient;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.MicrosoftSQLServer;

public class SqlServerDatabaseStrategy : IDatabaseConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;
    private readonly ILogger<SqlServerDatabaseStrategy> _logger;

    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.MicrosoftSqlServer;

    public SqlServerDatabaseStrategy(
        PasswordEncryptionService encryptionService,
        ILogger<SqlServerDatabaseStrategy> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

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
            { AuthenticationTypeEnum.WindowsIntegrated, true },
            { AuthenticationTypeEnum.Kerberos, true },
            { AuthenticationTypeEnum.NoAuth, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{source.Host},{source.Port}",
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

            case AuthenticationTypeEnum.Kerberos:
                builder.IntegratedSecurity = true;
                if (!string.IsNullOrEmpty(source.Principal))
                {
                    // For Kerberos, you might need additional configuration
                    // This is a simplified implementation
                }
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
            DataSource = $"{source.Host},{source.Port}",
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
                builder.Password = _encryptionService.Decrypt(source.Password);
                break;

            case AuthenticationTypeEnum.WindowsIntegrated:
                builder.IntegratedSecurity = true;
                break;

            case AuthenticationTypeEnum.AzureAdPassword:
                if (string.IsNullOrEmpty(source.Username) || string.IsNullOrEmpty(source.Password))
                    throw new ArgumentException("Username and password are required for Azure AD Password authentication.");
                builder.UserID = source.Username;
                builder.Password = _encryptionService.Decrypt(source.Password);
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

            case AuthenticationTypeEnum.Kerberos:
                builder.IntegratedSecurity = true;
                if (!string.IsNullOrEmpty(source.Principal))
                {
                    // For Kerberos, you might need additional configuration
                    // This is a simplified implementation
                }
                break;

            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for SQL Server.");
        }

        return builder.ConnectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        _logger.LogInformation("SQL Server: Testing connection with connection string: {ConnectionString}", 
            connectionString.Substring(0, Math.Min(100, connectionString.Length)) + "...");
        
        try
        {
            using var connection = new SqlConnection(connectionString);
            _logger.LogInformation("SQL Server: Opening connection...");
            await connection.OpenAsync();
            
            _logger.LogInformation("SQL Server: Connection opened successfully, executing test query...");
            // Test with a simple query
            using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync();
            
            var success = result != null && result.ToString() == "1";
            _logger.LogInformation("SQL Server: Test query result: {Result}, success: {Success}", result, success);
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}