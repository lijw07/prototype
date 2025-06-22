using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IDatabaseConnectionStrategy
{
    DataSourceTypeEnum DatabaseType { get; }
    string BuildConnectionString(ConnectionSourceDto source);
    string BuildConnectionString(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(string connectionString);
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
}