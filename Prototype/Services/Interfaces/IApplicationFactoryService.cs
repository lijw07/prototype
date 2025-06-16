using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IApplicationFactoryService
{
    ApplicationModel CreateApplication(ApplicationRequestDto requestDto);
    ApplicationModel UpdateApplication(ApplicationModel application, ApplicationRequestDto requestDto);
}