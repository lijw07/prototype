using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ApplicationSettingsController(
    IUnitOfWorkFactoryService uows,
    IAuthenticatedUserAccessor userAccessor,
    IEntityCreationFactoryService entityCreationFactory) : ControllerBase
{

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();

        var application = entityCreationFactory.CreateApplication(dto);
        var affectedEntities = new List<string>
        {
            nameof(ApplicationModel),
            nameof(UserModel),
            nameof(UserApplicationModel),
            nameof(ApplicationLogModel)
        };

        var applicationLog = entityCreationFactory.CreateApplicationLog(application, ApplicationActionTypeEnum.ApplicationAdded, affectedEntities);
        var userApplication = entityCreationFactory.CreateUserApplication(user, application);
        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Create, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Create, affectedEntities);
        
        await uows.Applications.AddAsync(application);
        await uows.ApplicationLogs.AddAsync(applicationLog);
        await uows.UserApplications.AddAsync(userApplication);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Successfully created application(s)" });
    }

    [HttpGet("get-applications")]
    public async Task<IActionResult> GetApplications()
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();

        var userAppIds = await uows.UserApplications
            .Query()
            .Where(ua => ua.UserId == user.UserId)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        var apps = await uows.Applications
            .Query()
            .Where(app => userAppIds.Contains(app.ApplicationId))
            .ToListAsync();
        
        var affectedEntities = new List<string>
        {
            nameof(ApplicationModel),
            nameof(UserApplicationModel)
        };

        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Read, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Read, affectedEntities);

        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        return Ok(apps);
    }

    [HttpPut("update-application/{applicationId}")]
    public async Task<IActionResult> UpdateApplication(Guid applicationId, [FromBody] ApplicationRequestDto dto)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();

        var userApp = await uows.UserApplications
            .Query()
            .Include(ua => ua.Application)
            .ThenInclude(app => app.ApplicationConnections)
            .FirstOrDefaultAsync(ua => ua.UserId == user.UserId && ua.ApplicationId == applicationId);

        if (userApp?.Application == null)
            return NotFound("Application not associated with the user.");

        var updatedApp = entityCreationFactory.UpdateApplication(userApp.Application, dto);
        var affectedEntities = new List<string> { nameof(ApplicationModel) };

        var log = entityCreationFactory.CreateApplicationLog(updatedApp, ApplicationActionTypeEnum.ApplicationUpdated, affectedEntities);
        var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Update, HttpContext);
        var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Update, affectedEntities);

        await uows.ApplicationLogs.AddAsync(log);
        await uows.UserActivityLogs.AddAsync(activity);
        await uows.AuditLogs.AddAsync(audit);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Application updated." });
    }

    [HttpDelete("delete-application/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(Guid applicationId)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();

        var userApp = await uows.UserApplications
            .Query()
            .Include(ua => ua.Application)
            .ThenInclude(app => app.ApplicationConnections)
            .FirstOrDefaultAsync(ua => ua.UserId == user.UserId && ua.ApplicationId == applicationId);

        if (userApp?.Application == null)
            return NotFound("Application not found or not associated with the user.");

        var affectedEntities = new List<string>
        {
            nameof(UserApplicationModel),
            nameof(ApplicationModel),
            nameof(ApplicationConnectionModel)
        };

        var log = entityCreationFactory.CreateApplicationLog(userApp.Application, ApplicationActionTypeEnum.ApplicationRemoved, affectedEntities);
        var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Delete, HttpContext);
        var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Delete, affectedEntities);

        uows.Applications.Delete(userApp.Application);
        await uows.ApplicationLogs.AddAsync(log);
        await uows.UserActivityLogs.AddAsync(activity);
        await uows.AuditLogs.AddAsync(audit);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Application deleted." });
    }
}