using Microsoft.AspNetCore.Mvc;

namespace Prototype.Controllers.Settings;


[ApiController]
[Route("[controller]")]
public class UserActivitySettingsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        throw new NotImplementedException();
    }
}