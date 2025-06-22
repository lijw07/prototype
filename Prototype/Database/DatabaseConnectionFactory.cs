using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database;

public interface IDatabaseConnectionFactory
{
    string BuildConnectionString(DataSourceTypeEnum databaseType, ConnectionSourceDto source);
    string BuildConnectionString(DataSourceTypeEnum databaseType, ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(DataSourceTypeEnum databaseType, string connectionString);
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    private readonly IEnumerable<IDatabaseConnectionStrategy> _strategies;
    private readonly ILogger<DatabaseConnectionFactory> _logger;

    public DatabaseConnectionFactory(
        IEnumerable<IDatabaseConnectionStrategy> strategies,
        ILogger<DatabaseConnectionFactory> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

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
            _logger.LogError(ex, "Failed to test connection for {DatabaseType}", databaseType);
            return false;
        }
    }

    private IDatabaseConnectionStrategy GetStrategy(DataSourceTypeEnum databaseType)
    {
        var strategy = _strategies.FirstOrDefault(s => s.DatabaseType == databaseType);
        if (strategy == null)
        {
            throw new NotSupportedException($"Database type '{databaseType}' is not supported.");
        }
        return strategy;
    }
}