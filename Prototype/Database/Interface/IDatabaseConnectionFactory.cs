using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IDatabaseConnectionFactory
{
    string BuildConnectionString(DataSourceTypeEnum databaseType, ConnectionSourceDto source);
    string BuildConnectionString(DataSourceTypeEnum databaseType, ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(DataSourceTypeEnum databaseType, string connectionString);
}