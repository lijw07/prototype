using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IFileConnectionStrategy
{
    DataSourceTypeEnum ConnectionType { get; }
    Dictionary<AuthenticationTypeEnum, bool> GetSupportedAuthTypes();
    Task<object> ReadDataAsync(ConnectionSourceDto source);
    Task<object> ReadDataAsync(ApplicationConnectionModel source);
    Task<bool> TestConnectionAsync(ConnectionSourceDto source);
    Task<bool> TestConnectionAsync(ApplicationConnectionModel source);
    string GetConnectionDescription();
}