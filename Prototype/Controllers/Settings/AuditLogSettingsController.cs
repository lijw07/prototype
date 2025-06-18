using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;

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
            .Select(log => new AuditLogDto
            {
                AuditLogId = log.AuditLogId,
                UserId = log.UserId,
                Username = log.User!.Username,
                ActionType = log.ActionType,
                Metadata = log.Metadata,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync();

        return Ok(logs);
    }
}