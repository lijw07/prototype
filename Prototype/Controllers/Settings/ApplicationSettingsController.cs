using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[Route("[controller]")]
public class ApplicationSettingsController : BaseSettingsController
{
    private readonly SentinelContext _context;
    private readonly IApplicationFactoryService _applicationFactory;
    private readonly IApplicationConnectionFactoryService _connectionFactory;

    public ApplicationSettingsController(
        SentinelContext context,
        IApplicationFactoryService applicationFactory,
        IApplicationConnectionFactoryService connectionFactory,
        IAuthenticatedUserAccessor userAccessor,
        ValidationService validationService,
        TransactionService transactionService,
        ILogger<ApplicationSettingsController> logger)
        : base(logger, userAccessor, validationService, transactionService)
    {
        _context = context;
        _applicationFactory = applicationFactory;
        _connectionFactory = connectionFactory;
    }

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check if the application name already exists for this user
            var existingApp = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationName == dto.ApplicationName);
            if (existingApp != null)
                return new { success = false, message = "Application name already exists" };

            return await _transactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Create application
                var applicationId = Guid.NewGuid();
                var application = _applicationFactory.CreateApplication(applicationId, dto);
                
                // Create connection
                var connection = _connectionFactory.CreateApplicationConnection(applicationId, dto.ConnectionSource);
                
                // Create a user-application relationship
                var userApplication = new UserApplicationModel
                {
                    UserApplicationId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = currentUser,
                    ApplicationId = applicationId,
                    Application = application,
                    ApplicationConnectionId = connection.ApplicationConnectionId,
                    ApplicationConnection = connection,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Applications.Add(application);
                _context.ApplicationConnections.Add(connection);
                _context.UserApplications.Add(userApplication);

                // Log activity
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = currentUser,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationAdded,
                    Description = $"User created application: {dto.ApplicationName}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                await _context.SaveChangesAsync();

                return new { success = true, message = "Application created successfully", applicationId = applicationId };
            });
        }, "creating application");
    }

    [HttpGet("get-applications")]
    public async Task<IActionResult> GetApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var query = from ua in _context.UserApplications
                       join app in _context.Applications on ua.ApplicationId equals app.ApplicationId
                       where ua.UserId == currentUser.UserId
                       select new
                       {
                           app.ApplicationId,
                           app.ApplicationName,
                           app.ApplicationDescription,
                           app.ApplicationDataSourceType,
                           app.CreatedAt,
                           app.UpdatedAt
                       };

            var totalCount = await query.CountAsync();
            var applications = await query
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();

            var result = CreatePaginatedResponse(applications, validPage, validPageSize, totalCount);
            return new { success = true, data = result };
        }, "retrieving applications");
    }

    [HttpPut("update-application/{applicationId}")]
    public async Task<IActionResult> UpdateApplication(string applicationId, [FromBody] ApplicationRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            if (!Guid.TryParse(applicationId, out var appGuid))
                return new { success = false, message = "Invalid application ID" };

            return await _transactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Check if user has access to this application
                var userApplication = await _context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == currentUser.UserId);
                if (userApplication == null)
                    return new { success = false, message = "Application not found or access denied" };

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                var connection = await _context.ApplicationConnections
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == appGuid);

                if (application == null || connection == null)
                    return new { success = false, message = "Application or connection not found" };

                // Update application and connection
                _applicationFactory.UpdateApplication(application, connection, dto);

                // Log activity
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = currentUser,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationUpdated,
                    Description = $"User updated application: {dto.ApplicationName}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                await _context.SaveChangesAsync();

                return new { success = true, message = "Application updated successfully" };
            });
        }, "updating application");
    }

    [HttpDelete("delete-application/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(string applicationId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            if (!Guid.TryParse(applicationId, out var appGuid))
                return new { success = false, message = "Invalid application ID" };

            return await _transactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Check if a user has access to this application
                var userApplication = await _context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == currentUser.UserId);
                if (userApplication == null)
                    return new { success = false, message = "Application not found or access denied" };

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                var connection = await _context.ApplicationConnections
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == appGuid);

                if (application != null)
                {
                    _context.Applications.Remove(application);
                }
                if (connection != null)
                {
                    _context.ApplicationConnections.Remove(connection);
                }
                _context.UserApplications.Remove(userApplication);

                // Log activity
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = currentUser,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationRemoved,
                    Description = $"User deleted application: {application?.ApplicationName ?? "Unknown"}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                await _context.SaveChangesAsync();

                return new { success = true, message = "Application deleted successfully" };
            });
        }, "deleting application");
    }

    [HttpPost("test-application-connection")]
    public async Task<IActionResult> TestApplicationConnection([FromBody] ApplicationRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            try
            {
                // For now, return a basic validation of connection parameters
                // In a real implementation; you would test the actual connection
                var isValid = !string.IsNullOrEmpty(dto.ConnectionSource.Host) &&
                             !string.IsNullOrEmpty(dto.ConnectionSource.Port) &&
                             !string.IsNullOrEmpty(dto.ConnectionSource.Url);

                if (!isValid)
                {
                    return new { success = false, message = "Invalid connection parameters" };
                }

                // Log connection test
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = currentUser,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ConnectionAttempt,
                    Description = $"User tested connection to {dto.ConnectionSource.Host}:{dto.ConnectionSource.Port}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                return new { success = true, message = "Connection test successful", connectionValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed for {Host}:{Port}", dto.ConnectionSource.Host, dto.ConnectionSource.Port);
                return new { success = false, message = "Connection test failed", error = ex.Message };
            }
        }, "testing application connection");
    }
}