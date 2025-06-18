using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prototype.Database.MicrosoftSQLServer;
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
    IEntityCreationFactoryService entityCreationFactory,
    SqlServerConnectionStrategy serverConnectionStrategy) : ControllerBase
{

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();

        var validationResult = ValidateApplicationRequest(dto);
        if (validationResult != null)
            return validationResult;

        var application = entityCreationFactory.CreateApplication(dto);
        var affectedEntities = new List<string>
        {
            nameof(ApplicationModel),
            nameof(UserModel),
            nameof(UserApplicationModel),
            nameof(ApplicationLogModel)
        };

        var userApplication = entityCreationFactory.CreateUserApplication(user, application);
        var applicationLog = entityCreationFactory.CreateApplicationLog(application, ActionTypeEnum.ApplicationAdded, affectedEntities);
        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ApplicationAdded, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ApplicationAdded, affectedEntities);
        
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

        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.Get, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.Get, affectedEntities);

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

        var log = entityCreationFactory.CreateApplicationLog(updatedApp, ActionTypeEnum.ApplicationUpdated, affectedEntities);
        var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ApplicationUpdated, HttpContext);
        var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ApplicationUpdated, affectedEntities);

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

        var log = entityCreationFactory.CreateApplicationLog(userApp.Application, ActionTypeEnum.ApplicationRemoved, affectedEntities);
        var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ApplicationRemoved, HttpContext);
        var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ApplicationRemoved, affectedEntities);

        uows.Applications.Delete(userApp.Application);
        await uows.ApplicationLogs.AddAsync(log);
        await uows.UserActivityLogs.AddAsync(activity);
        await uows.AuditLogs.AddAsync(audit);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Application deleted." });
    }
    
    [HttpPost("test-application-connection")]
    public async Task<IActionResult> TestApplicationConnection([FromBody] ApplicationRequestDto dto)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null) return Unauthorized();
        
        var validationResult = ValidateApplicationRequest(dto);
        if (validationResult != null)
            return validationResult;

        if (dto.DataSourceType != DataSourceTypeEnum.MicrosoftSqlServer)
            return BadRequest(new { message = "Only SQL Server supported for now." });
        
        var affected = new List<string> { nameof(ApplicationConnectionModel) };

        try
        {
            var connStr = serverConnectionStrategy.Build(dto.ConnectionSource);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            
            var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ConnectionSuccess, HttpContext);
            var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ConnectionSuccess, affected);
            
            await uows.UserActivityLogs.AddAsync(activity);
            await uows.AuditLogs.AddAsync(audit);
            await uows.SaveChangesAsync();
            
            return Ok(new { message = "Connection successful!" });
        }
        catch (Exception ex)
        {
            var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ConnectionFailure, HttpContext);
            var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ConnectionFailure, affected);
            
            await uows.UserActivityLogs.AddAsync(activity);
            await uows.AuditLogs.AddAsync(audit);
            await uows.SaveChangesAsync();
            
            return BadRequest(new { message = "Connection failed", error = ex.Message });
        }
    }
    
    [HttpPost("test-application-connection/{connectionId:guid}")]
    public async Task<IActionResult> TestSingleConnection(Guid connectionId)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        if (user == null)
            return Unauthorized();
        
        // More efficient: get user's app IDs first, then check connection
        var myAppIds = await uows.UserApplications
            .Query()
            .Where(ua => ua.UserId == user.UserId)
            .Select(ua => ua.ApplicationId)
            .ToListAsync();

        var appConn = await uows.ApplicationConnections
            .Query()
            .Include(ac => ac.Application)
            .FirstOrDefaultAsync(ac =>
                ac.ApplicationConnectionId == connectionId &&
                myAppIds.Contains(ac.Application.ApplicationId));

        if (appConn is null)
            return NotFound(new { message = "Connection not found or not yours." });
        
        var application = appConn.Application;

        if (application is null)
            return NotFound(new { message = "Application not found." });
        
        var affected = new List<string> { nameof(ApplicationConnectionModel) };
        
        try
        {
            var connStr = serverConnectionStrategy.Build(appConn);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            
            var log = entityCreationFactory.CreateApplicationLog(application, ActionTypeEnum.ConnectionSuccess, affected);
            var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ConnectionSuccess, HttpContext);
            var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ConnectionSuccess, affected);

            await uows.ApplicationLogs.AddAsync(log);
            await uows.UserActivityLogs.AddAsync(activity);
            await uows.AuditLogs.AddAsync(audit);
            await uows.SaveChangesAsync();
            
            return Ok(new
            {
                message = "Connection OK!",
                application = new
                {
                    application.ApplicationId,
                    application.ApplicationName,
                    application.ApplicationDescription
                }
            });
        }
        catch (SqlException sqlEx)
        {
            var log = entityCreationFactory.CreateApplicationLog(application, ActionTypeEnum.ConnectionFailure, affected);
            var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ConnectionFailure, HttpContext);
            var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ConnectionFailure, affected);

            await uows.ApplicationLogs.AddAsync(log);
            await uows.UserActivityLogs.AddAsync(activity);
            await uows.AuditLogs.AddAsync(audit);
            return BadRequest(new { message = "Connection failed.", error = sqlEx.Message });
        }
        catch (Exception ex)
        {
            
            var log = entityCreationFactory.CreateApplicationLog(application, ActionTypeEnum.ConnectionFailure, affected);
            var activity = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ConnectionFailure, HttpContext);
            var audit = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ConnectionFailure, affected);

            await uows.ApplicationLogs.AddAsync(log);
            await uows.UserActivityLogs.AddAsync(activity);
            await uows.AuditLogs.AddAsync(audit);
            return BadRequest(new { message = "Connection failed.", error = ex.Message });
        }
    }
    
    // Private helper to validate ApplicationRequestDto for CreateApplication
    private IActionResult? ValidateApplicationRequest(ApplicationRequestDto dto)
    {
        var connection = dto.ConnectionSource;

        static IActionResult Bad(string msg) => new BadRequestObjectResult(new { message = msg });

        if (string.IsNullOrWhiteSpace(dto.ApplicationName)) return Bad("Application name cannot be empty.");
        if (string.IsNullOrWhiteSpace(connection.Host)) return Bad("Host cannot be empty.");
        if (string.IsNullOrWhiteSpace(connection.Port)) return Bad("Port cannot be empty.");
        if (string.IsNullOrWhiteSpace(connection.DatabaseName)) return Bad("Database name cannot be empty.");

        var requiresUsername = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.AzureAdInteractive,
            AuthenticationTypeEnum.AzureAdMsi,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256
        }.Contains(connection.AuthenticationType);

        var requiresPassword = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256
        }.Contains(connection.AuthenticationType);

        if (requiresUsername && string.IsNullOrWhiteSpace(connection.Username))
            return Bad("Username cannot be empty.");
        if (requiresPassword && string.IsNullOrWhiteSpace(connection.Password))
            return Bad("Password cannot be empty.");

        if (connection.AuthenticationType == AuthenticationTypeEnum.AwsIam)
        {
            if (string.IsNullOrWhiteSpace(connection.AwsAccessKeyId)) return Bad("AWS Access Key ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(connection.AwsSecretAccessKey)) return Bad("AWS Secret Access Key cannot be empty.");
            if (string.IsNullOrWhiteSpace(connection.AwsSessionToken)) return Bad("AWS Session Token cannot be empty.");
        }

        if ((connection.AuthenticationType == AuthenticationTypeEnum.ScramSha1 ||
             connection.AuthenticationType == AuthenticationTypeEnum.ScramSha256) &&
            string.IsNullOrWhiteSpace(connection.AuthenticationDatabase))
        {
            return Bad("Authentication Database cannot be empty.");
        }

        if (connection.AuthenticationType == AuthenticationTypeEnum.GssapiKerberos)
        {
            if (string.IsNullOrWhiteSpace(connection.Principal)) return Bad("Principal cannot be empty.");
            if (string.IsNullOrWhiteSpace(connection.ServiceName)) return Bad("Service Name cannot be empty.");
            if (string.IsNullOrWhiteSpace(connection.ServiceRealm)) return Bad("Service Realm cannot be empty.");
        }

        return null;
    }
}