using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IApiConnectionStrategy
{
    DataSourceTypeEnum ConnectionType { get; }
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
    Task<object> ExecuteRequestAsync(ConnectionSourceDto source);
    Task<object> ExecuteRequestAsync(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(ConnectionSourceDto source);
    Task<bool> TestConnectionAsync(ApplicationConnectionModel source);
    string GetConnectionDescription();
}