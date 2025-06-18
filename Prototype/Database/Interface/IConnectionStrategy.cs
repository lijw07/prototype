using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.Interface;

public interface IConnectionStrategy
{
    bool CanHandle(AuthenticationTypeEnum type);
    string Build(ConnectionSourceDto source);
    string Build(ApplicationConnectionModel source);
}