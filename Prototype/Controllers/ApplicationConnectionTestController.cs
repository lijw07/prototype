using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Database.Interface;
using Prototype.Utility;
using Prototype.Controllers.Navigation;

namespace Prototype.Controllers;

[Route("api/application-connection-test")]
public class ApplicationConnectionTestController(
    ILogger<ApplicationConnectionTestController> logger,
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    IDatabaseConnectionFactory dbConnectionFactory,
    IEnumerable<IApiConnectionStrategy> apiStrategies,
    IEnumerable<IFileConnectionStrategy> fileStrategies)
    : BaseNavigationController(logger, context, userAccessor, null!, null!)
{
    private readonly IDatabaseConnectionFactory _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
    private readonly IEnumerable<IApiConnectionStrategy> _apiStrategies = apiStrategies ?? throw new ArgumentNullException(nameof(apiStrategies));
    private readonly IEnumerable<IFileConnectionStrategy> _fileStrategies = fileStrategies ?? throw new ArgumentNullException(nameof(fileStrategies));

    [HttpPost("debug-connection-test")]
    public async Task<IActionResult> DebugConnectionTest([FromBody] object requestData)
    {
        return Ok(new { success = false, message = "DEBUG METHOD CALLED - This proves routing works", connectionValid = false });
    }
    
    [HttpPost("test-application-connection")]
    public async Task<IActionResult> TestApplicationConnection([FromBody] object requestData)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            try
            {
                string host, port, description;
                var connectionTestResult = false;
                DataSourceTypeEnum dataSourceType;
                Guid? testingApplicationId = null;
                ApplicationConnectionModel? existingConnection = null;
                ConnectionSourceDto? newConnectionSource = null;
                
                // Check if this is a request by applicationId or full data
                var jsonElement = (JsonElement)requestData;
                Logger.LogInformation("Received connection test request: {JsonData}", jsonElement.GetRawText());
                
                if (jsonElement.TryGetProperty("applicationId", out var appIdProperty) && appIdProperty.ValueKind == JsonValueKind.String)
                {
                    // Testing existing application by ID
                    var applicationId = Guid.Parse(appIdProperty.GetString()!);
                    testingApplicationId = applicationId;
                    
                    // Get the application and its connection
                    var userApp = await Context!.UserApplications
                        .Include(ua => ua.Application)
                        .Include(ua => ua.ApplicationConnection)
                        .FirstOrDefaultAsync(ua => ua.ApplicationId == applicationId && ua.UserId == currentUser.UserId);
                    
                    if (userApp?.ApplicationConnection == null)
                        return BadRequest(new { success = false, message = "Application or connection not found" });
                    
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
                        var dto = JsonSerializer.Deserialize<ApplicationRequestDto>(jsonElement.GetRawText(), options);
                        Logger.LogInformation("Deserialized DTO: ApplicationName={ApplicationName}, DataSourceType={DataSourceType}, ConnectionSource null={ConnectionSourceNull}", 
                            dto?.ApplicationName, dto?.DataSourceType, dto?.ConnectionSource == null);
                        
                        if (dto?.ConnectionSource == null)
                        {
                            Logger.LogWarning("ConnectionSource is null in deserialized DTO");
                            return BadRequest(new { success = false, message = "Invalid connection data" });
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
                        return BadRequest(new { success = false, message = "Failed to parse connection data" });
                    }
                    
                    // Basic validation for required fields
                    var isValid = !string.IsNullOrEmpty(newConnectionSource.Url);
                    if (!isValid)
                    {
                        return BadRequest(new { success = false, message = "Connection URL is required" });
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
                            connectionString = _dbConnectionFactory.BuildConnectionString(dataSourceType, existingConnection);
                        }
                        else if (newConnectionSource != null)
                        {
                            Logger.LogInformation("Building connection string from new connection source");
                            connectionString = _dbConnectionFactory.BuildConnectionString(dataSourceType, newConnectionSource);
                        }
                        else
                        {
                            Logger.LogWarning("No connection data available for testing");
                            return BadRequest(new { success = false, message = "No connection data available" });
                        }
                        
                        Logger.LogInformation("Built connection string, testing connection...");
                        connectionTestResult = await _dbConnectionFactory.TestConnectionAsync(dataSourceType, connectionString);
                        Logger.LogInformation("Database connection test result: {Result}", connectionTestResult);
                    }
                    else if (IsApiType(dataSourceType))
                    {
                        // Test API connection
                        var apiStrategy = _apiStrategies.FirstOrDefault(s => s.ConnectionType == dataSourceType);
                        if (apiStrategy == null)
                        {
                            return BadRequest(new { success = false, message = $"API connection type '{dataSourceType}' is not supported" });
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
                            return BadRequest(new { success = false, message = "No connection data available" });
                        }
                    }
                    else if (IsFileType(dataSourceType))
                    {
                        // Test file connection
                        var fileStrategy = _fileStrategies.FirstOrDefault(s => s.ConnectionType == dataSourceType);
                        if (fileStrategy == null)
                        {
                            return BadRequest(new { success = false, message = $"File connection type '{dataSourceType}' is not supported" });
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
                            return BadRequest(new { success = false, message = "No connection data available" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { success = false, message = $"Connection type '{dataSourceType}' is not supported" });
                    }
                }
                catch (Exception testEx)
                {
                    Logger.LogError(testEx, "Connection test failed for {DataSourceType}: {Error}", dataSourceType, testEx.Message);
                    connectionTestResult = false;
                }
                
                Logger.LogInformation("Final connection test result for {DataSourceType}: {Result}", dataSourceType, connectionTestResult);

                // Log connection test attempt
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                    ActionType = ActionTypeEnum.ConnectionAttempt,
                    Description = $"User {description} - Result: {(connectionTestResult ? "Success" : "Failed")}",
                    Timestamp = DateTime.UtcNow
                };
                Context!.UserActivityLogs.Add(activityLog);

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
                    Context!.ApplicationLogs.Add(applicationLog);
                    
                    Logger.LogInformation("Added application log for connection test on application {ApplicationId} with result {Result}", testingApplicationId.Value, connectionTestResult);
                }

                await Context!.SaveChangesAsync();

                var message = connectionTestResult ? "Connection test successful" : "Connection test failed";
                return Ok(new { success = connectionTestResult, message = message, connectionValid = connectionTestResult });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Connection test failed: {Error}", ex.Message);
                return StatusCode(500, new { success = false, message = "Connection test failed", error = ex.Message });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in connection test");
            return StatusCode(500, new { success = false, message = "An unexpected error occurred" });
        }
    }

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