using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using ApplicationModel = Microsoft.AspNetCore.Mvc.ApplicationModels.ApplicationModel;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
public class ApplicationSettingsController(
    IAuthenticatedUserAccessor userAccessor,
    IUnitOfWorkService uows,
    IEntityCreationFactoryService entityCreationFactory): ControllerBase
{
    
    private UserModel? _user;

    private async Task<UserModel?> GetCurrentUserAsync()
    {
        if (_user == null)
            _user = await userAccessor.GetUserFromTokenAsync(User);
        return _user;
    }

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        var application = entityCreationFactory.CreateApplication(dto);
        
        var affectedEntities = new List<string>
        {
            nameof(ApplicationModel),
            nameof(UserModel),
            nameof(UserApplicationModel),
            nameof(ApplicationLogModel)
        };
        
        var applicationLog = entityCreationFactory.CreateApplicationLog(application, ApplicationActionTypeEnum.ApplicationAdded, affectedEntities);
        
        var user = await GetCurrentUserAsync();
        
        if (user == null)
            return Unauthorized();

        var userApplication = entityCreationFactory.CreateUserApplication(user, application);
        
        user.Applications.Add(userApplication);
        
        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Create, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Create, affectedEntities);

        await uows.Applications.AddAsync(application);
        await uows.ApplicationLogs.AddAsync(applicationLog);
        await uows.UserApplications.AddAsync(userApplication);
        await uows.Users.AddAsync(user);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();
        
        return Ok(new { message = "Successfully created application(s)" });
    }
}