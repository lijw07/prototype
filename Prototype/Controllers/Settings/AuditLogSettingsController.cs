using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers.Settings;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AuditLogSettingsController(SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!UserHasPlatformAdminPermission())
            return Forbid("Access denied. Platform_Admin permission is required.");

        var logs = await context.AuditLogs
            .Include(log => log.User)
            .OrderByDescending(log => log.CreatedAt)
            .ToListAsync();

        return Ok(logs);
    }

    private bool UserHasPlatformAdminPermission()
    {
        return User.Claims
            .Where(c => c.Type == "permission")
            .Any(c => c.Value == "Platform_Admin");
    }
}