using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                    User = null, // Don't set navigation property to avoid tracking issues
                    ApplicationId = applicationId,
                    Application = null, // Don't set navigation property to avoid tracking issues
                    ApplicationConnectionId = connection.ApplicationConnectionId,
                    ApplicationConnection = null, // Don't set navigation property to avoid tracking issues
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
                    User = null, // Don't set navigation property to avoid tracking issues
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationAdded,
                    Description = $"User created application: {dto.ApplicationName}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                // Also create application log
                var applicationLog = new ApplicationLogModel
                {
                    ApplicationLogId = Guid.NewGuid(),
                    ApplicationId = applicationId,
                    Application = null, // Don't set navigation property to avoid tracking issues
                    ActionType = ActionTypeEnum.ApplicationAdded,
                    Metadata = $"Application '{dto.ApplicationName}' was created by user {currentUser.Username}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ApplicationLogs.Add(applicationLog);
                
                _logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", applicationId, ActionTypeEnum.ApplicationAdded);

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
                       join conn in _context.ApplicationConnections on ua.ApplicationConnectionId equals conn.ApplicationConnectionId
                       where ua.UserId == currentUser.UserId
                       select new
                       {
                           app.ApplicationId,
                           app.ApplicationName,
                           app.ApplicationDescription,
                           app.ApplicationDataSourceType,
                           app.CreatedAt,
                           app.UpdatedAt,
                           Connection = new
                           {
                               conn.Host,
                               conn.Port,
                               conn.DatabaseName,
                               AuthenticationType = conn.AuthenticationType.ToString()
                           }
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
                    User = null, // Don't set navigation property to avoid tracking issues
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationUpdated,
                    Description = $"User updated application: {dto.ApplicationName}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                // Also create application log
                var applicationLog = new ApplicationLogModel
                {
                    ApplicationLogId = Guid.NewGuid(),
                    ApplicationId = appGuid,
                    Application = null, // Don't set navigation property to avoid tracking issues
                    ActionType = ActionTypeEnum.ApplicationUpdated,
                    Metadata = $"Application '{dto.ApplicationName}' was updated by user {currentUser.Username}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ApplicationLogs.Add(applicationLog);
                
                _logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", appGuid, ActionTypeEnum.ApplicationUpdated);

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

                // Check if any other users are still using this application connection
                var otherUsersUsingConnection = await _context.UserApplications
                    .Where(ua => ua.ApplicationConnectionId == userApplication.ApplicationConnectionId && ua.UserId != currentUser.UserId)
                    .CountAsync();

                var otherUsersUsingApp = await _context.UserApplications
                    .Where(ua => ua.ApplicationId == appGuid && ua.UserId != currentUser.UserId)
                    .CountAsync();

                _logger.LogInformation("Found {OtherUsersConnection} other users using connection, {OtherUsersApp} other users using application {AppId}", 
                    otherUsersUsingConnection, otherUsersUsingApp, appGuid);

                // Store the application name for logging before potential deletion
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                var applicationName = application?.ApplicationName ?? "Unknown";

                // Create logs first before making any deletions
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null, // Don't set navigation property to avoid tracking issues
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ApplicationRemoved,
                    Description = $"User deleted application: {applicationName}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                // Create application log before any deletions
                var applicationLog = new ApplicationLogModel
                {
                    ApplicationLogId = Guid.NewGuid(),
                    ApplicationId = appGuid,
                    Application = null, // Don't set navigation property to avoid tracking issues
                    ActionType = ActionTypeEnum.ApplicationRemoved,
                    Metadata = otherUsersUsingApp > 0 
                        ? $"User {currentUser.Username} removed their access to application '{applicationName}'"
                        : $"Application '{applicationName}' was permanently deleted by user {currentUser.Username}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ApplicationLogs.Add(applicationLog);
                
                _logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", appGuid, ActionTypeEnum.ApplicationRemoved);

                // Save logs first
                await _context.SaveChangesAsync();

                // Now remove the current user's relationship
                _context.UserApplications.Remove(userApplication);
                await _context.SaveChangesAsync();

                // Only delete the connection if no other users are using it
                if (otherUsersUsingConnection == 0)
                {
                    var connection = await _context.ApplicationConnections
                        .FirstOrDefaultAsync(ac => ac.ApplicationConnectionId == userApplication.ApplicationConnectionId);
                    if (connection != null)
                    {
                        _context.ApplicationConnections.Remove(connection);
                        await _context.SaveChangesAsync();
                    }
                }

                // DO NOT delete the application from the database to preserve application logs
                // The application record will remain in the database for audit/logging purposes
                // Only the user's access (UserApplication relationship) has been removed above

                return new { success = true, message = "Application deleted successfully" };
            });
        }, "deleting application");
    }

    [HttpPost("test-application-connection")]
    public async Task<IActionResult> TestApplicationConnection([FromBody] object requestData)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            try
            {
                string host, port, description;
                Guid? testingApplicationId = null;
                
                // Check if this is a request by applicationId or full data
                var jsonElement = (System.Text.Json.JsonElement)requestData;
                
                if (jsonElement.TryGetProperty("applicationId", out var appIdProperty) && appIdProperty.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // Testing existing application by ID
                    var applicationId = Guid.Parse(appIdProperty.GetString()!);
                    testingApplicationId = applicationId;
                    
                    // Get the application and its connection
                    var userApp = await _context.UserApplications
                        .Include(ua => ua.Application)
                        .Include(ua => ua.ApplicationConnection)
                        .FirstOrDefaultAsync(ua => ua.ApplicationId == applicationId && ua.UserId == currentUser.UserId);
                    
                    if (userApp?.ApplicationConnection == null)
                        return new { success = false, message = "Application or connection not found" };
                    
                    host = userApp.ApplicationConnection.Host;
                    port = userApp.ApplicationConnection.Port;
                    description = $"tested connection to existing application {userApp.Application?.ApplicationName}";
                }
                else
                {
                    // Testing new application with full connection data
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new JsonStringEnumConverter());
                    var dto = System.Text.Json.JsonSerializer.Deserialize<ApplicationRequestDto>(jsonElement.GetRawText(), options);
                    if (dto?.ConnectionSource == null)
                        return new { success = false, message = "Invalid connection data" };
                    
                    host = dto.ConnectionSource.Host;
                    port = dto.ConnectionSource.Port;
                    description = $"tested connection to new application {dto.ApplicationName}";
                    
                    // Validate required fields for new applications
                    var isValid = !string.IsNullOrEmpty(host) &&
                                 !string.IsNullOrEmpty(port) &&
                                 !string.IsNullOrEmpty(dto.ConnectionSource.Url);

                    if (!isValid)
                    {
                        return new { success = false, message = "Invalid connection parameters" };
                    }
                }

                // Log connection test
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null, // Don't set navigation property to avoid tracking issues
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ConnectionAttempt,
                    Description = $"User {description}",
                    Timestamp = DateTime.UtcNow
                };
                _context.UserActivityLogs.Add(activityLog);

                // Add application log if testing existing application
                if (testingApplicationId.HasValue)
                {
                    var applicationLog = new ApplicationLogModel
                    {
                        ApplicationLogId = Guid.NewGuid(),
                        ApplicationId = testingApplicationId.Value,
                        Application = null, // Don't set navigation property to avoid tracking issues
                        ActionType = ActionTypeEnum.ConnectionAttempt,
                        Metadata = $"Connection test performed by user {currentUser.Username}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ApplicationLogs.Add(applicationLog);
                    
                    _logger.LogInformation("Added application log for connection test on application {ApplicationId}", testingApplicationId.Value);
                }

                await _context.SaveChangesAsync();

                return new { success = true, message = "Connection test successful", connectionValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed: {Error}", ex.Message);
                return new { success = false, message = "Connection test failed", error = ex.Message };
            }
        }, "testing application connection");
    }
}