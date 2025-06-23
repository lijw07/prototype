using System.Data.Odbc;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Oracle;

public class OracleDatabaseStrategy : IDatabaseConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;
    private readonly ILogger<OracleDatabaseStrategy> _logger;

    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.Oracle;

    public OracleDatabaseStrategy(
        PasswordEncryptionService encryptionService,
        ILogger<OracleDatabaseStrategy> logger)
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
            { AuthenticationTypeEnum.WindowsIntegrated, false },
            { AuthenticationTypeEnum.AzureAdPassword, false },
            { AuthenticationTypeEnum.AzureAdIntegrated, false }
        };
    }

    public string BuildConnectionString(ConnectionSourceDto source)
    {
        var connectionString = $"DRIVER={{Oracle in XE}};DBQ={source.Host}:{source.Port}/{source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                connectionString += $"UID={source.Username};PWD={source.Password};";
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for Oracle.");
        }

        return connectionString;
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var connectionString = $"DRIVER={{Oracle in XE}};DBQ={source.Host}:{source.Port}/{source.DatabaseName};";

        switch (source.AuthenticationType)
        {
            case AuthenticationTypeEnum.UserPassword:
                var password = string.IsNullOrEmpty(source.Password) ? "" : _encryptionService.Decrypt(source.Password);
                connectionString += $"UID={source.Username};PWD={password};";
                break;
                
            default:
                throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported for Oracle.");
        }

        return connectionString;
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var connection = new OdbcConnection(connectionString);
            await connection.OpenAsync();
            
            using var command = new OdbcCommand("SELECT 1 FROM DUAL", connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oracle ODBC connection test failed: {Error}", ex.Message);
            return false;
        }
    }
}