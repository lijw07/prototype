using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Controllers.Navigation;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[Route("navigation/user-requests")]
[ApiController]
public class UserRequestsNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ILogger<UserRequestsNavigationController> logger)
    : BaseNavigationController(logger, context, userAccessor)
{

    [HttpGet]
    public async Task<IActionResult> GetUserRequests()
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                var userRequests = await Context.UserRequests
                    .Where(ur => ur.UserId == currentUser.UserId)
                    .OrderByDescending(ur => ur.RequestedAt)
                    .Select(ur => new
                    {
                        id = ur.UserRequestId.ToString(),
                        toolName = ur.ToolName,
                        toolCategory = ur.ToolCategory,
                        reason = ur.Reason,
                        status = ur.Status.ToString().ToLower(),
                        requestedAt = ur.RequestedAt,
                        reviewedAt = ur.ReviewedAt,
                        reviewedBy = ur.ReviewedBy,
                        comments = ur.Comments,
                        priority = ur.Priority.ToString().ToLower(),
                        userId = ur.UserId
                    })
                    .ToListAsync();

                return SuccessResponse(userRequests, "User requests retrieved successfully");
            }, "retrieving user requests");
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateUserRequestDto request)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                if (string.IsNullOrWhiteSpace(request.ToolName) || string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequestWithMessage("Tool name and reason are required");

                // Parse priority enum
                if (!System.Enum.TryParse<RequestPriorityEnum>(request.Priority, true, out var priority))
                    priority = RequestPriorityEnum.Medium;

                // Create new user request
                var userRequest = new UserRequestModel
                {
                    UserRequestId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    ToolId = request.ToolId,
                    ToolName = request.ToolName,
                    ToolCategory = request.ToolCategory,
                    Reason = request.Reason,
                    Status = UserRequestStatusEnum.Pending,
                    Priority = priority,
                    RequestedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Context.UserRequests.Add(userRequest);
                await Context.SaveChangesAsync();

                // Log the request for audit purposes
                await LogUserActionAsync(currentUser.UserId, ActionTypeEnum.ApplicationAdded, 
                    $"User requested access to {request.ToolName}", 
                    $"User requested access to {request.ToolName}");

                var responseData = new
                {
                    id = userRequest.UserRequestId.ToString(),
                    toolName = userRequest.ToolName,
                    toolCategory = userRequest.ToolCategory,
                    reason = userRequest.Reason,
                    status = userRequest.Status.ToString().ToLower(),
                    requestedAt = userRequest.RequestedAt,
                    priority = userRequest.Priority.ToString().ToLower(),
                    userId = userRequest.UserId
                };

                return SuccessResponse(new { data = responseData }, "Request submitted successfully");
            }, "creating user request");
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequestById(string id)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                if (!Guid.TryParse(id, out var requestId))
                    return BadRequestWithMessage("Invalid request ID");

                var userRequest = await Context.UserRequests
                    .Where(ur => ur.UserRequestId == requestId && ur.UserId == currentUser.UserId)
                    .Select(ur => new
                    {
                        id = ur.UserRequestId.ToString(),
                        toolName = ur.ToolName,
                        toolCategory = ur.ToolCategory,
                        reason = ur.Reason,
                        status = ur.Status.ToString().ToLower(),
                        requestedAt = ur.RequestedAt,
                        reviewedAt = ur.ReviewedAt,
                        reviewedBy = ur.ReviewedBy,
                        comments = ur.Comments,
                        priority = ur.Priority.ToString().ToLower(),
                        userId = ur.UserId
                    })
                    .FirstOrDefaultAsync();

                if (userRequest == null)
                    return BadRequestWithMessage("Request not found");

                return SuccessResponse(userRequest, "Request retrieved successfully");
            }, "retrieving request by ID");
        });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateRequestStatus(string id, [FromBody] UpdateRequestStatusDto request)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Check if user has admin rights
                if (currentUser.Role != "Admin")
                    return BadRequestWithMessage("Only administrators can update request status");

                if (!Guid.TryParse(id, out var requestId))
                    return BadRequestWithMessage("Invalid request ID");

                if (!System.Enum.TryParse<UserRequestStatusEnum>(request.Status, true, out var status))
                    return BadRequestWithMessage("Invalid status");

                var userRequest = await Context.UserRequests
                    .FirstOrDefaultAsync(ur => ur.UserRequestId == requestId);

                if (userRequest == null)
                    return BadRequestWithMessage("Request not found");

                userRequest.Status = status;
                userRequest.ReviewedAt = DateTime.UtcNow;
                userRequest.ReviewedBy = currentUser.Email;
                userRequest.Comments = request.Comments;
                userRequest.UpdatedAt = DateTime.UtcNow;

                await Context.SaveChangesAsync();

                await LogUserActionAsync(currentUser.UserId, ActionTypeEnum.Update, 
                    $"Updated request {id} status to {request.Status}", 
                    $"Updated request {id} status to {request.Status}");

                var responseData = new
                {
                    id = userRequest.UserRequestId.ToString(),
                    status = userRequest.Status.ToString().ToLower(),
                    reviewedAt = userRequest.ReviewedAt,
                    reviewedBy = userRequest.ReviewedBy,
                    comments = userRequest.Comments
                };

                return SuccessResponse(new { data = responseData }, "Request status updated successfully");
            }, "updating request status");
        });
    }

    [HttpGet("available-tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var availableTools = new[]
                {
                    new
                    {
                        category = "Development Tools",
                        tools = new[]
                        {
                            new { id = "github", name = "GitHub Repository Access", description = "Access to company GitHub repositories", requiresApproval = true },
                            new { id = "gitlab", name = "GitLab Access", description = "Access to GitLab projects and repositories", requiresApproval = true },
                            new { id = "jira", name = "Jira Project Access", description = "Access to specific Jira projects", requiresApproval = true },
                            new { id = "confluence", name = "Confluence Spaces", description = "Access to documentation spaces", requiresApproval = false }
                        }
                    },
                    new
                    {
                        category = "Database Access",
                        tools = new[]
                        {
                            new { id = "prod-db", name = "Production Database", description = "Read-only access to production database", requiresApproval = true },
                            new { id = "staging-db", name = "Staging Database", description = "Full access to staging database", requiresApproval = true },
                            new { id = "analytics-db", name = "Analytics Database", description = "Access to analytics and reporting database", requiresApproval = true }
                        }
                    },
                    new
                    {
                        category = "Cloud Services",
                        tools = new[]
                        {
                            new { id = "aws-console", name = "AWS Console Access", description = "Access to AWS management console", requiresApproval = true },
                            new { id = "azure-portal", name = "Azure Portal", description = "Access to Azure cloud services", requiresApproval = true },
                            new { id = "gcp-console", name = "Google Cloud Console", description = "Access to Google Cloud Platform", requiresApproval = true }
                        }
                    }
                };

                return SuccessResponse(availableTools, "Available tools retrieved successfully");
            }, "retrieving available tools");
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelRequest(string id)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                if (!Guid.TryParse(id, out var requestId))
                    return BadRequestWithMessage("Invalid request ID");

                var userRequest = await Context.UserRequests
                    .FirstOrDefaultAsync(ur => ur.UserRequestId == requestId && ur.UserId == currentUser.UserId);

                if (userRequest == null)
                    return BadRequestWithMessage("Request not found");

                // Only allow cancellation of pending requests
                if (userRequest.Status != UserRequestStatusEnum.Pending)
                    return BadRequestWithMessage("Only pending requests can be cancelled");

                userRequest.Status = UserRequestStatusEnum.Cancelled;
                userRequest.UpdatedAt = DateTime.UtcNow;

                await Context.SaveChangesAsync();

                await LogUserActionAsync(currentUser.UserId, ActionTypeEnum.ApplicationRemoved, 
                    $"Cancelled request {id}", 
                    $"Cancelled request {id}");

                return SuccessResponse(new { message = "Request cancelled successfully" }, "Request cancelled successfully");
            }, "cancelling request");
        });
    }

}

public class CreateUserRequestDto
{
    public string ToolId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string ToolCategory { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Priority { get; set; }
}

public class UpdateRequestStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
}