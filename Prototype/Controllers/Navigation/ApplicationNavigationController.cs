using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/applications")]
public class ApplicationNavigationController(
    SentinelContext context,
    IApplicationFactoryService applicationFactory,
    IApplicationConnectionFactoryService connectionFactory,
    IDatabaseConnectionFactory dbConnectionFactory,
    IEnumerable<IApiConnectionStrategy> apiStrategies,
    IEnumerable<IFileConnectionStrategy> fileStrategies,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IHttpContextAccessor httpContextAccessor,
    IHttpContextParsingService httpContextParsingService,
    ILogger<ApplicationNavigationController> logger,
    IAuditLogService auditLogService)
    : BaseNavigationController(logger, userAccessor, auditLogService, transactionService)
{
    private readonly IAuditLogService _auditLogService = auditLogService;

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            // Check if the application name already exists for this user
            var existingApp = await context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationName == dto.ApplicationName);
            if (existingApp != null)
                return BadRequestWithMessage("Application name already exists");

            return await TransactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Create application
                var applicationId = Guid.NewGuid();
                var application = applicationFactory.CreateApplication(applicationId, dto);
                
                // Create connection
                var connection = connectionFactory.CreateApplicationConnection(applicationId, dto.ConnectionSource);
                
                // Create a user-application relationship
                var userApplication = new UserApplicationModel
                {
                    UserApplicationId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null!, // Don't set navigation property to avoid tracking issues
                    ApplicationId = applicationId,
                    Application = null!, // Don't set navigation property to avoid tracking issues
                    ApplicationConnectionId = connection.ApplicationConnectionId,
                    ApplicationConnection = null!, // Don't set navigation property to avoid tracking issues
                    CreatedAt = DateTime.UtcNow
                };

                context.Applications.Add(application);
                context.ApplicationConnections.Add(connection);
                context.UserApplications.Add(userApplication);

                // Create activity log using centralized service
                var httpContext = httpContextAccessor.HttpContext;
                var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
                var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);
            
                await _auditLogService.CreateUserActivityLogAsync(currentUser.UserId, ActionTypeEnum.ApplicationAdded, "Created new application", ipAddress, deviceInfo);
                
                Logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", applicationId, ActionTypeEnum.ApplicationAdded);

                await context.SaveChangesAsync();

                return SuccessResponse("Application created successfully");
            });
        }, "creating application");
    }

    [HttpGet("get-applications")]
    public async Task<IActionResult> GetApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var query = from ua in context.UserApplications
                       join app in context.Applications on ua.ApplicationId equals app.ApplicationId
                       join conn in context.ApplicationConnections on ua.ApplicationConnectionId equals conn.ApplicationConnectionId
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
                               host = conn.Host,
                               port = conn.Port,
                               databaseName = conn.DatabaseName,
                               authenticationType = conn.AuthenticationType.ToString(),
                               username = conn.Username,
                               // Additional fields based on authentication type
                               authenticationDatabase = conn.AuthenticationDatabase,
                               awsAccessKeyId = conn.AwsAccessKeyId,
                               principal = conn.Principal,
                               serviceName = conn.ServiceName,
                               serviceRealm = conn.ServiceRealm,
                               canonicalizeHostName = conn.CanonicalizeHostName
                               // Note: Password and secret keys are intentionally not returned for security
                           }
                       };

            var totalCount = await query.CountAsync();
            var applications = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();


            var result = CreatePaginatedResponse(applications, validPage, validPageSize, totalCount);
            return SuccessResponse(result);
        }, "retrieving applications");
    }

    [HttpPut("update-application/{applicationId}")]
    public async Task<IActionResult> UpdateApplication(string applicationId, [FromBody] ApplicationRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            if (!Guid.TryParse(applicationId, out var appGuid))
                return BadRequestWithMessage("Invalid application ID");

            return await TransactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Check if user has access to this application
                var userApplication = await context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == currentUser.UserId);
                if (userApplication == null)
                    return new { success = false, message = "Application not found or access denied" };

                var application = await context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                var connection = await context.ApplicationConnections
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == appGuid);

                if (application == null || connection == null)
                    return new { success = false, message = "Application or connection not found" };

                // Update application and connection
                applicationFactory.UpdateApplication(application, connection, dto);

                // Create activity log using centralized service
                var httpContext = httpContextAccessor.HttpContext;
                var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
                var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);
            
                await _auditLogService.CreateUserActivityLogAsync(currentUser.UserId, ActionTypeEnum.ApplicationUpdated, "Update new application", ipAddress, deviceInfo);

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
                context.ApplicationLogs.Add(applicationLog);
                
                Logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", appGuid, ActionTypeEnum.ApplicationUpdated);

                await context.SaveChangesAsync();

                return new { success = true, message = "Application updated successfully" };
            });
        }, "updating application");
    }

    [HttpDelete("delete-application/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(string applicationId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            if (!Guid.TryParse(applicationId, out var appGuid))
                return new { success = false, message = "Invalid application ID" };

            return await TransactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Check if a user has access to this application
                var userApplication = await context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == currentUser.UserId);
                if (userApplication == null)
                    return new { success = false, message = "Application not found or access denied" };

                // Check if any other users are still using this application connection
                var otherUsersUsingConnection = await context.UserApplications
                    .Where(ua => ua.ApplicationConnectionId == userApplication.ApplicationConnectionId && ua.UserId != currentUser.UserId)
                    .CountAsync();

                var otherUsersUsingApp = await context.UserApplications
                    .Where(ua => ua.ApplicationId == appGuid && ua.UserId != currentUser.UserId)
                    .CountAsync();

                Logger.LogInformation("Found {OtherUsersConnection} other users using connection, {OtherUsersApp} other users using application {AppId}", 
                    otherUsersUsingConnection, otherUsersUsingApp, appGuid);

                // Store the application name for logging before potential deletion
                var application = await context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                var applicationName = application?.ApplicationName ?? "Unknown";

                // Create logs first before making any deletions
                var httpContext = httpContextAccessor.HttpContext;
                var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
                var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);
            
                await _auditLogService.CreateUserActivityLogAsync(currentUser.UserId, ActionTypeEnum.Delete, "User Delete application", ipAddress, deviceInfo);

                // Create audit log for application deletion
                await _auditLogService.CreateAuditLogAsync(currentUser.UserId, ActionTypeEnum.Delete, "User Delete application");
                
                Logger.LogInformation("Added audit log for application {ApplicationId} deletion by user {UserId}", appGuid, currentUser.UserId);

                // Create application log only if we're not going to delete the application
                // (If we're deleting the application, the audit log and activity log will suffice)
                if (otherUsersUsingApp > 0)
                {
                    var applicationLog = new ApplicationLogModel
                    {
                        ApplicationLogId = Guid.NewGuid(),
                        ApplicationId = appGuid,
                        Application = null, // Don't set navigation property to avoid tracking issues
                        ActionType = ActionTypeEnum.ApplicationRemoved,
                        Metadata = $"User {currentUser.Username} removed their access to application '{applicationName}'",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.ApplicationLogs.Add(applicationLog);
                    
                    Logger.LogInformation("Added application log for application {ApplicationId} with action {ActionType}", appGuid, ActionTypeEnum.ApplicationRemoved);
                }

                // Now remove the current user's relationship
                context.UserApplications.Remove(userApplication);

                // Only delete the connection if no other users are using it
                if (otherUsersUsingConnection == 0)
                {
                    var connection = await context.ApplicationConnections
                        .FirstOrDefaultAsync(ac => ac.ApplicationConnectionId == userApplication.ApplicationConnectionId);
                    if (connection != null)
                    {
                        context.ApplicationConnections.Remove(connection);
                    }
                }

                // Only delete the application if no other users are using it
                if (otherUsersUsingApp == 0)
                {
                    // First, we need to delete all application logs for this application
                    var applicationLogs = await context.ApplicationLogs
                        .Where(al => al.ApplicationId == appGuid)
                        .ToListAsync();
                    
                    if (applicationLogs.Any())
                    {
                        context.ApplicationLogs.RemoveRange(applicationLogs);
                        Logger.LogInformation("Removing {LogCount} application logs before deleting application {ApplicationId}", 
                            applicationLogs.Count, appGuid);
                    }
                    
                    if (application != null)
                    {
                        context.Applications.Remove(application);
                        Logger.LogInformation("Application {ApplicationId} permanently deleted from database as no other users were using it", appGuid);
                    }
                }
                else
                {
                    Logger.LogInformation("Application {ApplicationId} kept in database as {OtherUsersCount} other users are still using it", appGuid, otherUsersUsingApp);
                }

                var message = otherUsersUsingApp > 0 
                    ? "Your access to the application has been removed" 
                    : "Application deleted successfully";
                
                return new { success = true, message = message };
            });
        }, "deleting application");
    }

    [HttpPost("test-application-connection")]
    public async Task<IActionResult> TestApplicationConnection([FromBody] object requestData)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            try
            {
                string host, port, description;
                bool connectionTestResult = false;
                DataSourceTypeEnum dataSourceType;
                Guid? testingApplicationId = null;
                ApplicationConnectionModel? existingConnection = null;
                ConnectionSourceDto? newConnectionSource = null;
                
                // Check if this is a request by applicationId or full data
                var jsonElement = (System.Text.Json.JsonElement)requestData;
                Logger.LogInformation("Received connection test request: {JsonData}", jsonElement.GetRawText());
                
                if (jsonElement.TryGetProperty("applicationId", out var appIdProperty) && appIdProperty.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // Testing existing application by ID
                    var applicationId = Guid.Parse(appIdProperty.GetString()!);
                    testingApplicationId = applicationId;
                    
                    // Get the application and its connection
                    var userApp = await context.UserApplications
                        .Include(ua => ua.Application)
                        .Include(ua => ua.ApplicationConnection)
                        .FirstOrDefaultAsync(ua => ua.ApplicationId == applicationId && ua.UserId == currentUser.UserId);
                    
                    if (userApp?.ApplicationConnection == null)
                        return new { success = false, message = "Application or connection not found" };
                    
                    existingConnection = userApp.ApplicationConnection;
                    host = existingConnection.Host;
                    port = existingConnection.Port;
                    description = $"tested connection to existing application {userApp.Application.ApplicationName}";
                    
                    // Get the data source type
                    dataSourceType = userApp.Application.ApplicationDataSourceType;
                }
                else
                {
                    Logger.LogInformation("Testing new application with full connection data");
                    
                    // Testing new application with full connection data
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    };
                    options.Converters.Add(new JsonStringEnumConverter());
                    
                    try
                    {
                        var dto = System.Text.Json.JsonSerializer.Deserialize<ApplicationRequestDto>(jsonElement.GetRawText(), options);
                        Logger.LogInformation("Deserialized DTO: ApplicationName={ApplicationName}, DataSourceType={DataSourceType}, ConnectionSource null={ConnectionSourceNull}", 
                            dto?.ApplicationName, dto?.DataSourceType, dto?.ConnectionSource == null);
                        
                        if (dto?.ConnectionSource == null)
                        {
                            Logger.LogWarning("ConnectionSource is null in deserialized DTO");
                            return new { success = false, message = "Invalid connection data" };
                        }
                        
                        newConnectionSource = dto.ConnectionSource;
                        host = dto.ConnectionSource.Host;
                        port = dto.ConnectionSource.Port;
                        description = $"tested connection to new application {dto.ApplicationName}";
                        dataSourceType = dto.DataSourceType;
                        
                        Logger.LogInformation("Parsed connection data: Host={Host}, Port={Port}, DataSourceType={DataSourceType}", 
                            host, port, dataSourceType);
                    }
                    catch (Exception deserializeEx)
                    {
                        Logger.LogError(deserializeEx, "Failed to deserialize connection test request");
                        return new { success = false, message = "Failed to parse connection data" };
                    }
                    
                    // Basic validation for required fields
                    var isValid = !string.IsNullOrEmpty(newConnectionSource.Url);
                    if (!isValid)
                    {
                        return new { success = false, message = "Connection URL is required" };
                    }
                }

                // Perform the actual connection test based on the data source type
                Logger.LogInformation("Testing connection for data source type: {DataSourceType}", dataSourceType);
                
                try
                {
                    if (IsDatabaseType(dataSourceType))
                    {
                        Logger.LogInformation("Testing database connection for {DataSourceType}", dataSourceType);
                        
                        // Test database connection
                        string connectionString;
                        if (existingConnection != null)
                        {
                            Logger.LogInformation("Building connection string from existing connection");
                            connectionString = dbConnectionFactory.BuildConnectionString(dataSourceType, existingConnection);
                        }
                        else if (newConnectionSource != null)
                        {
                            Logger.LogInformation("Building connection string from new connection source");
                            connectionString = dbConnectionFactory.BuildConnectionString(dataSourceType, newConnectionSource);
                        }
                        else
                        {
                            Logger.LogWarning("No connection data available for testing");
                            return new { success = false, message = "No connection data available" };
                        }
                        
                        Logger.LogInformation("Built connection string, testing connection...");
                        connectionTestResult = await dbConnectionFactory.TestConnectionAsync(dataSourceType, connectionString);
                        Logger.LogInformation("Database connection test result: {Result}", connectionTestResult);
                    }
                    else if (IsApiType(dataSourceType))
                    {
                        // Test API connection
                        var apiStrategy = apiStrategies.FirstOrDefault(s => s.ConnectionType == dataSourceType);
                        if (apiStrategy == null)
                        {
                            return new { success = false, message = $"API connection type '{dataSourceType}' is not supported" };
                        }
                        
                        if (existingConnection != null)
                        {
                            connectionTestResult = await apiStrategy.TestConnectionAsync(existingConnection);
                        }
                        else if (newConnectionSource != null)
                        {
                            connectionTestResult = await apiStrategy.TestConnectionAsync(newConnectionSource);
                        }
                        else
                        {
                            return new { success = false, message = "No connection data available" };
                        }
                    }
                    else if (IsFileType(dataSourceType))
                    {
                        // Test file connection
                        var fileStrategy = fileStrategies.FirstOrDefault(s => s.ConnectionType == dataSourceType);
                        if (fileStrategy == null)
                        {
                            return new { success = false, message = $"File connection type '{dataSourceType}' is not supported" };
                        }
                        
                        if (existingConnection != null)
                        {
                            connectionTestResult = await fileStrategy.TestConnectionAsync(existingConnection);
                        }
                        else if (newConnectionSource != null)
                        {
                            connectionTestResult = await fileStrategy.TestConnectionAsync(newConnectionSource);
                        }
                        else
                        {
                            return new { success = false, message = "No connection data available" };
                        }
                    }
                    else
                    {
                        return new { success = false, message = $"Connection type '{dataSourceType}' is not supported" };
                    }
                }
                catch (Exception testEx)
                {
                    Logger.LogError(testEx, "Connection test failed for {DataSourceType}: {Error}", dataSourceType, testEx.Message);
                    connectionTestResult = false;
                }
                
                Logger.LogInformation("Final connection test result for {DataSourceType}: {Result}", dataSourceType, connectionTestResult);

                // Create activity log using centralized service
                var httpContext = httpContextAccessor.HttpContext;
                var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
                var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);
            
                await _auditLogService.CreateUserActivityLogAsync(currentUser.UserId, ActionTypeEnum.ApplicationAdded, "Created new application", ipAddress, deviceInfo);

                // Add application log if testing existing application
                if (testingApplicationId.HasValue)
                {
                    var applicationLog = new ApplicationLogModel
                    {
                        ApplicationLogId = Guid.NewGuid(),
                        ApplicationId = testingApplicationId.Value,
                        Application = null,
                        ActionType = ActionTypeEnum.ConnectionAttempt,
                        Metadata = $"Connection test performed by user {currentUser.Username} - Result: {(connectionTestResult ? "Success" : "Failed")}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.ApplicationLogs.Add(applicationLog);
                    
                    Logger.LogInformation("Added application log for connection test on application {ApplicationId} with result {Result}", testingApplicationId.Value, connectionTestResult);
                }

                await context.SaveChangesAsync();

                var message = connectionTestResult ? "Connection test successful" : "Connection test failed";
                return new { success = connectionTestResult, message = message, connectionValid = connectionTestResult };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Connection test failed: {Error}", ex.Message);
                return new { success = false, message = "Connection test failed", error = ex.Message };
            }
        }, "testing application connection");
    }
    
    // REMOVED: Old test connection method - replaced with standardized TestApplicationConnection method above

    private static bool IsDatabaseType(DataSourceTypeEnum dataSourceType)
    {
        return dataSourceType is DataSourceTypeEnum.MicrosoftSqlServer or
               DataSourceTypeEnum.MySql or
               DataSourceTypeEnum.PostgreSql or
               DataSourceTypeEnum.MongoDb or
               DataSourceTypeEnum.Redis or
               DataSourceTypeEnum.Oracle or
               DataSourceTypeEnum.MariaDb or
               DataSourceTypeEnum.Sqlite or
               DataSourceTypeEnum.Cassandra or
               DataSourceTypeEnum.ElasticSearch;
    }

    private static bool IsApiType(DataSourceTypeEnum dataSourceType)
    {
        return dataSourceType is DataSourceTypeEnum.RestApi or
               DataSourceTypeEnum.GraphQL or
               DataSourceTypeEnum.SoapApi or
               DataSourceTypeEnum.ODataApi or
               DataSourceTypeEnum.WebSocket;
    }

    private static bool IsFileType(DataSourceTypeEnum dataSourceType)
    {
        return dataSourceType is DataSourceTypeEnum.CsvFile or
               DataSourceTypeEnum.JsonFile or
               DataSourceTypeEnum.XmlFile or
               DataSourceTypeEnum.ExcelFile or
               DataSourceTypeEnum.ParquetFile or
               DataSourceTypeEnum.YamlFile or
               DataSourceTypeEnum.TextFile;
    }
}