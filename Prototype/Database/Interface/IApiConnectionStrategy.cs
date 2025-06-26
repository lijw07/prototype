using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IApiConnectionStrategy
{
    DataSourceTypeEnum ConnectionType { get; }
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
    Task<object> ExecuteRequestAsync(ConnectionSourceRequestDto sourceRequest);
    Task<object> ExecuteRequestAsync(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(ConnectionSourceRequestDto sourceRequest);
    Task<bool> TestConnectionAsync(ApplicationConnectionModel source);
    string GetConnectionDescription();
}