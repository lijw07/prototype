using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IDatabaseConnectionFactory
{
    string BuildConnectionString(DataSourceTypeEnum databaseType, ConnectionSourceRequestDto sourceRequest);
    string BuildConnectionString(DataSourceTypeEnum databaseType, ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(DataSourceTypeEnum databaseType, string connectionString);
}