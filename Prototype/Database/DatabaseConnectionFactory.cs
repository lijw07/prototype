using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database;

public class DatabaseConnectionFactory(
    IEnumerable<IDatabaseConnectionStrategy> strategies,
    ILogger<DatabaseConnectionFactory> logger)
    : IDatabaseConnectionFactory
{
    public string BuildConnectionString(DataSourceTypeEnum databaseType, ConnectionSourceDto source)
    {
        var strategy = GetStrategy(databaseType);
        return strategy.BuildConnectionString(source);
    }

    public string BuildConnectionString(DataSourceTypeEnum databaseType, ApplicationConnectionModel source)
    {
        var strategy = GetStrategy(databaseType);
        return strategy.BuildConnectionString(source);
    }

    public async Task<bool> TestConnectionAsync(DataSourceTypeEnum databaseType, string connectionString)
    {
        try
        {
            var strategy = GetStrategy(databaseType);
            return await strategy.TestConnectionAsync(connectionString);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to test connection for {DatabaseType}", databaseType);
            return false;
        }
    }

    private IDatabaseConnectionStrategy GetStrategy(DataSourceTypeEnum databaseType)
    {
        var strategy = strategies.FirstOrDefault(s => s.DatabaseType == databaseType);
        if (strategy == null)
        {
            throw new NotSupportedException($"Database type '{databaseType}' is not supported.");
        }
        return strategy;
    }
}