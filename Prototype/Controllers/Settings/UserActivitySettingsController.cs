using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class UserActivitySettingsController(SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var logs = await context.UserActivityLogs
            .Include(log => log.User)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        return Ok(logs);
    }
}