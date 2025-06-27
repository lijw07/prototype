using Nest;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Database.ElasticSearch;

public class ElasticSearchDatabaseStrategy(
    IPasswordEncryptionService encryptionService,
    ILogger<ElasticSearchDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.ElasticSearch;

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
        var connectionInfo = new
        {
            Host = source.Host,
            Port = source.Port,
            Username = source.Username,
            Password = source.Password,
            AuthenticationType = source.AuthenticationType,
            Index = source.DatabaseName // ElasticSearch uses indices instead of databases
        };

        return System.Text.Json.JsonSerializer.Serialize(connectionInfo);
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var connectionInfo = new
        {
            Host = source.Host,
            Port = source.Port,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            AuthenticationType = source.AuthenticationType,
            Index = source.DatabaseName
        };

        return System.Text.Json.JsonSerializer.Serialize(connectionInfo);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var connectionInfo = System.Text.Json.JsonSerializer.Deserialize<dynamic>(connectionString);
            
            var host = connectionInfo.GetProperty("Host").GetString();
            var port = connectionInfo.GetProperty("Port").GetString();
            var authType = (AuthenticationTypeEnum)connectionInfo.GetProperty("AuthenticationType").GetInt32();

            var uri = new Uri($"http://{host}:{port}");
            var settings = new ConnectionSettings(uri);

            // Set authentication
            if (authType == AuthenticationTypeEnum.UserPassword)
            {
                var username = connectionInfo.GetProperty("Username").GetString();
                var password = connectionInfo.GetProperty("Password").GetString();
                
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    settings.BasicAuthentication(username, password);
                }
            }

            // Set timeout for connection test
            settings.RequestTimeout(TimeSpan.FromSeconds(10));
            settings.DisableDirectStreaming(); // For better error messages

            var client = new ElasticClient(settings);
            
            // Test connectivity by getting cluster health
            var response = await client.Cluster.HealthAsync();
            
            return response.IsValid && response.ApiCall.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ElasticSearch connection test failed");
            return false;
        }
    }
}