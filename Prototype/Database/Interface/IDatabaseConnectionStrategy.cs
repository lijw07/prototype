using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IDatabaseConnectionStrategy
{
    DataSourceTypeEnum DatabaseType { get; }
    string BuildConnectionString(ConnectionSourceRequestDto sourceRequest);
    string BuildConnectionString(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(string connectionString);
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
}