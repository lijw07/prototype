using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IApplicationFactoryService
{
    ApplicationModel CreateApplication(Guid applicationGuid, ApplicationRequestDto requestDto);
    ApplicationModel UpdateApplication(ApplicationModel application, ApplicationConnectionModel connectionSource, ApplicationRequestDto requestDto);
}