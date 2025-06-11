using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class ApplicationSettingsController : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        throw new NotImplementedException();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetApplication(int id)
    {
        throw new NotImplementedException();
    }

    [HttpPut("applications/{id}")]
    public async Task<IActionResult> UpdateApplication(int id, [FromBody] ApplicationRequestDto dto)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("applications/{id}")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        throw new NotImplementedException();
    }
}