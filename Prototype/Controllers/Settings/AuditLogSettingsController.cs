using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class AuditLogSettingsController : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        throw new NotImplementedException();
    }
}