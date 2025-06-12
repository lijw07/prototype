using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class ApplicationSettingsController(
    IAuthenticatedUserAccessor userAccessor,
    IUnitOfWorkService uows,
    IJwtTokenService jwtTokenService,
    IEntityCreationFactoryService entityCreationFactory): ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        throw new NotImplementedException();
    }
}