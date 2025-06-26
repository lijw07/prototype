using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IFileConnectionStrategy
{
    DataSourceTypeEnum ConnectionType { get; }
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
    Task<object> ReadDataAsync(ConnectionSourceRequestDto sourceRequest);
    Task<object> ReadDataAsync(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(ConnectionSourceRequestDto sourceRequest);
    Task<bool> TestConnectionAsync(ApplicationConnectionModel source);
    string GetConnectionDescription();
}