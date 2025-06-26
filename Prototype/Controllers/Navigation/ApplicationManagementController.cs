using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("settings/applications")]
public class ApplicationManagementController : BaseApiController
{
    private readonly IApplicationFactoryService _applicationFactory;
    private readonly IApplicationConnectionFactoryService _connectionFactory;

    public ApplicationManagementController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        IApplicationFactoryService applicationFactory,
        IApplicationConnectionFactoryService connectionFactory,
        ILogger<ApplicationManagementController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
        _applicationFactory = applicationFactory ?? throw new ArgumentNullException(nameof(applicationFactory));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    [HttpPost("new-application-connection")]
    public async Task<IActionResult> CreateApplication([FromBody] ApplicationRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Check if the application name already exists for this user
                var existingApp = await Context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationName == dto.ApplicationName);

                if (existingApp != null)
                {
                    return BadRequestWithMessage($"Application with name '{dto.ApplicationName}' already exists");
                }

                // Create new application using factory
                var application = _applicationFactory.CreateApplication(
                    Guid.NewGuid(),
                    dto);

                Context.Applications.Add(application);

                // Create application connection using factory
                var connection = _connectionFactory.CreateApplicationConnection(
                    application.ApplicationId,
                    dto.ConnectionSource);

                Context.ApplicationConnections.Add(connection);

                // Create user-application relationship
                var userApplication = new UserApplicationModel
                {
                    UserApplicationId = Guid.NewGuid(),
                    UserId = user.UserId,
                    User = null!, // Don't set navigation property to avoid tracking issues
                    ApplicationId = application.ApplicationId,
                    Application = null!, // Don't set navigation property to avoid tracking issues
                    ApplicationConnectionId = connection.ApplicationConnectionId,
                    ApplicationConnection = null!, // Don't set navigation property to avoid tracking issues
                    CreatedAt = DateTime.UtcNow
                };

                Context.UserApplications.Add(userApplication);

                return SuccessResponse(new
                {
                    ApplicationId = application.ApplicationId,
                    ApplicationName = application.ApplicationName,
                    Message = "Application created successfully"
                }, "Application created successfully");

            }, ActionTypeEnum.ApplicationAdded, 
               $"Created application '{dto.ApplicationName}'",
               "Application created successfully");
        });
    }

    [HttpGet("get-applications")]
    public async Task<IActionResult> GetApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (Context == null)
                throw new InvalidOperationException("Database context is not available");

            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var totalCount = await Context.UserApplications
                .Where(ua => ua.UserId == currentUser.UserId)
                .CountAsync();

            var userApplications = await Context.UserApplications
                .Where(ua => ua.UserId == currentUser.UserId)
                .Include(ua => ua.Application)
                .ThenInclude(a => a!.Connections)
                .OrderByDescending(ua => ua.CreatedAt)
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();

            var applicationDtos = userApplications.Select(ua => new ApplicationDto
            {
                ApplicationId = ua.Application!.ApplicationId,
                ApplicationName = ua.Application.ApplicationName,
                Description = ua.Application.ApplicationDescription ?? string.Empty,
                CreatedAt = ua.Application.CreatedAt,
                CreatedBy = Guid.Empty, // ApplicationModel doesn't have CreatedBy
                Connections = ua.Application.Connections?.Select(ac => new ApplicationConnectionDto
                {
                    ApplicationConnectionId = ac.ApplicationConnectionId,
                    ConnectionString = ac.Url,
                    DatabaseStrategy = ac.AuthenticationType.ToString(),
                    Schema = ac.DatabaseName,
                    Port = ac.Port,
                    Host = ac.Host
                }).ToList() ?? new List<ApplicationConnectionDto>()
            }).ToList();

            var result = CreatePaginatedResponse(applicationDtos, validPage, validPageSize, totalCount);
            return SuccessResponse(result, "Applications retrieved successfully");
        });
    }

    [HttpPut("update-application/{applicationId}")]
    public async Task<IActionResult> UpdateApplication(string applicationId, [FromBody] ApplicationRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (!Guid.TryParse(applicationId, out var appGuid))
            {
                return BadRequestWithMessage("Invalid application ID");
            }

            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Verify user has access to this application
                var userApplication = await Context.UserApplications
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == user.UserId);

                if (userApplication == null)
                {
                    return BadRequestWithMessage("Application not found or access denied");
                }

                // Get the application
                var application = await Context.Applications
                    .Include(a => a.Connections)
                    .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);

                if (application == null)
                {
                    return BadRequestWithMessage("Application not found");
                }

                // Update application properties
                application.ApplicationName = dto.ApplicationName;
                application.ApplicationDescription = dto.ApplicationDescription ?? string.Empty;
                application.UpdatedAt = DateTime.UtcNow;

                // Update connection if exists
                var connection = application.Connections?.FirstOrDefault();
                if (connection != null)
                {
                    connection.Url = dto.ConnectionSource.Url;
                    connection.AuthenticationType = dto.ConnectionSource.AuthenticationType;
                    connection.DatabaseName = dto.ConnectionSource.DatabaseName;
                    connection.Port = dto.ConnectionSource.Port;
                    connection.Host = dto.ConnectionSource.Host;
                    connection.UpdatedAt = DateTime.UtcNow;
                }

                return SuccessResponse(new
                {
                    ApplicationId = application.ApplicationId,
                    ApplicationName = application.ApplicationName,
                    Message = "Application updated successfully"
                }, "Application updated successfully");

            }, ActionTypeEnum.ApplicationUpdated,
               $"Updated application '{dto.ApplicationName}'",
               "Application updated successfully");
        });
    }

    [HttpDelete("delete-application/{applicationId}")]
    public async Task<IActionResult> DeleteApplication(string applicationId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (!Guid.TryParse(applicationId, out var appGuid))
            {
                return BadRequestWithMessage("Invalid application ID");
            }

            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Verify user has access to this application
                var userApplication = await Context.UserApplications
                    .Include(ua => ua.Application)
                    .FirstOrDefaultAsync(ua => ua.ApplicationId == appGuid && ua.UserId == user.UserId);

                if (userApplication == null)
                {
                    return BadRequestWithMessage("Application not found or access denied");
                }

                var applicationName = userApplication.Application!.ApplicationName;

                // Remove user-application relationship
                Context.UserApplications.Remove(userApplication);

                // Check if other users are associated with this application
                var otherUserApps = await Context.UserApplications
                    .Where(ua => ua.ApplicationId == appGuid && ua.UserId != user.UserId)
                    .AnyAsync();

                // If no other users, delete the application and its connections
                if (!otherUserApps)
                {
                    var application = await Context.Applications
                        .Include(a => a.Connections)
                        .FirstOrDefaultAsync(a => a.ApplicationId == appGuid);

                    if (application != null)
                    {
                        // Delete connections first
                        if (application.Connections != null)
                        {
                            Context.ApplicationConnections.RemoveRange(application.Connections);
                        }

                        // Delete application
                        Context.Applications.Remove(application);
                    }
                }

                return SuccessResponse(new
                {
                    ApplicationId = appGuid,
                    Message = "Application access removed successfully"
                }, "Application deleted successfully");

            }, ActionTypeEnum.ApplicationRemoved,
               $"Removed application '{applicationId}'",
               "Application deleted successfully");
        });
    }
}