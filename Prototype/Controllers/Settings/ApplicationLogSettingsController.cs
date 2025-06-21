using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers.Settings;

[Route("[controller]")]
public class ApplicationLogSettingsController : BaseSettingsController
{
    private readonly SentinelContext _context;

    public ApplicationLogSettingsController(
        SentinelContext context, 
        ILogger<ApplicationLogSettingsController> logger)
        : base(logger)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var skip = (page - 1) * pageSize;

            var logs = await _context.ApplicationLogs
                .Include(log => log.Application)
                .OrderByDescending(log => log.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(log => new
                {
                    ApplicationLogId = log.ApplicationLogId,
                    ApplicationId = log.ApplicationId,
                    ApplicationName = log.Application != null ? log.Application.ApplicationName : "[Deleted Application]",
                    ActionType = log.ActionType,
                    Metadata = log.Metadata,
                    CreatedAt = log.CreatedAt,
                    UpdatedAt = log.UpdatedAt
                })
                .ToListAsync();

            var totalCount = await _context.ApplicationLogs.CountAsync();

            var result = CreatePaginatedResponse(logs, page, pageSize, totalCount);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application logs");
            return StatusCode(500, new { success = false, message = "An internal error occurred" });
        }
    }
}