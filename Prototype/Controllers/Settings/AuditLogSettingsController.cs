using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers.Settings;

[Route("[controller]")]
public class AuditLogSettingsController : BaseSettingsController
{
    private readonly SentinelContext _context;

    public AuditLogSettingsController(SentinelContext context, ILogger<AuditLogSettingsController> logger)
        : base(logger)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            _logger.LogInformation("Getting audit logs - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;

            var totalCount = await _context.AuditLogs.CountAsync();

            var logs = await _context.AuditLogs
                .Include(log => log.User)
                .OrderByDescending(log => log.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(log => new
                {
                    AuditLogId = log.AuditLogId,
                    UserId = log.UserId,
                    Username = log.User != null ? log.User.Username : "Unknown User",
                    ActionType = log.ActionType,
                    Metadata = log.Metadata,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync();
            
            var result = CreatePaginatedResponse(logs, page, pageSize, totalCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { message = "An internal error occurred" });
        }
    }
}