using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Cache;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Navigation;

[Route("[controller]")]
public class ApplicationLogNavigationController(
    SentinelContext context,
    ICacheService cacheService,
    ILogger<ApplicationLogNavigationController> logger)
    : BaseNavigationController(logger)
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            // Check cache first
            var cacheKey = $"application-logs:page:{page}:size:{pageSize}";
            var cachedLogs = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedLogs != null)
            {
                Logger.LogDebug("Application logs cache hit for page: {Page}, pageSize: {PageSize}", page, pageSize);
                return Ok(new { success = true, data = cachedLogs });
            }

            var skip = (page - 1) * pageSize;

            var logs = await context.ApplicationLogs
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

            var totalCount = await context.ApplicationLogs.CountAsync();

            var result = CreatePaginatedResponse(logs, page, pageSize, totalCount);

            // Cache for 8 minutes
            await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(8));
            Logger.LogDebug("Application logs cached for page: {Page}, pageSize: {PageSize}", page, pageSize);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving application logs");
            return StatusCode(500, new { success = false, message = "An internal error occurred" });
        }
    }
}