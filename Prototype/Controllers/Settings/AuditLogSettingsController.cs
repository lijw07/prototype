using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class AuditLogSettingsController(SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var logs = await context.AuditLogs
            .Include(log => log.User)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();

        return Ok(logs);
    }
}