using System.Text.Json;
using Cassandra;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.Cassandra;

public class CassandraDatabaseStrategy(
    PasswordEncryptionService encryptionService,
    ILogger<CassandraDatabaseStrategy> logger)
    : IDatabaseConnectionStrategy
{
    public DataSourceTypeEnum DatabaseType => DataSourceTypeEnum.Cassandra;

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
        // Cassandra doesn't use traditional connection strings, but we'll build a connection info string
        var contactPoints = sourceRequest.Host.Split(',').Select(h => h.Trim()).ToArray();
        var port = int.Parse(sourceRequest.Port);
        var keyspace = sourceRequest.DatabaseName;

        var connectionInfo = new
        {
            ContactPoints = contactPoints,
            Port = port,
            Keyspace = keyspace,
            Username = sourceRequest.Username,
            Password = sourceRequest.Password,
            AuthenticationType = sourceRequest.AuthenticationType
        };

        return System.Text.Json.JsonSerializer.Serialize(connectionInfo);
    }

    public string BuildConnectionString(ApplicationConnectionModel source)
    {
        var contactPoints = source.Host.Split(',').Select(h => h.Trim()).ToArray();
        var port = int.Parse(source.Port);
        var keyspace = source.DatabaseName;

        var connectionInfo = new
        {
            ContactPoints = contactPoints,
            Port = port,
            Keyspace = keyspace,
            Username = source.Username,
            Password = string.IsNullOrEmpty(source.Password) ? null : encryptionService.Decrypt(source.Password),
            AuthenticationType = source.AuthenticationType
        };

        return System.Text.Json.JsonSerializer.Serialize(connectionInfo);
    }

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var connectionInfo = JsonDocument.Parse(connectionString).RootElement;
            
            var cluster = Cluster.Builder();
            
            // Add contact points
            if (connectionInfo.TryGetProperty("ContactPoints", out JsonElement contactPointsElement))
            {
                var contactPoints = contactPointsElement.EnumerateArray()
                    .Select(cp => cp.GetString())
                    .Where(cp => !string.IsNullOrEmpty(cp))
                    .ToArray();
                cluster.AddContactPoints(contactPoints);
            }

            // Set port
            if (connectionInfo.TryGetProperty("Port", out JsonElement portElement))
            {
                cluster.WithPort(portElement.GetInt32());
            }

            // Set authentication
            if (connectionInfo.TryGetProperty("AuthenticationType", out JsonElement authElement) &&
                authElement.GetInt32() == (int)AuthenticationTypeEnum.UserPassword)
            {
                if (connectionInfo.TryGetProperty("Username", out JsonElement usernameElement) &&
                    connectionInfo.TryGetProperty("Password", out JsonElement passwordElement))
                {
                    var username = usernameElement.GetString();
                    var password = passwordElement.GetString();
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        cluster.WithCredentials(username, password);
                    }
                }
            }

            using var session = await cluster.Build().ConnectAsync();
            
            // Test basic connectivity
            var statement = new SimpleStatement("SELECT release_version FROM system.local");
            var result = await session.ExecuteAsync(statement);
            
            return result.FirstOrDefault() != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cassandra connection test failed");
            return false;
        }
    }
}