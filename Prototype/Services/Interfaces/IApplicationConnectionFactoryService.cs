using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IApplicationConnectionFactoryService
{
    ApplicationConnectionModel CreateApplicationConnection(Guid applicationId, ConnectionSourceDto dto);
}