using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class UserSettingsController() : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        throw new NotImplementedException();
    }
    
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UserRequestDto dto)
    {
        throw new NotImplementedException();
    }
}