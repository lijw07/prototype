using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers;

[Authorize]
[Route("api/user-requests")]
[ApiController]
public class UserRequestsController : ControllerBase
{
    private readonly SentinelContext _context;
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly ILogger<UserRequestsController> _logger;

    public UserRequestsController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        ILogger<UserRequestsController> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserRequests()
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var userRequests = await _context.UserRequests
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

            return Ok(new { success = true, data = userRequests });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user requests");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateUserRequestDto request)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrWhiteSpace(request.ToolName) || string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "Tool name and reason are required" });

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

            _context.UserRequests.Add(userRequest);
            await _context.SaveChangesAsync();

            // Log the request for audit purposes
            await LogUserActivity(currentUser.UserId, ActionTypeEnum.ApplicationAdded, 
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

            return Ok(new { success = true, data = responseData, message = "Request submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user request");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRequestById(string id)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            if (!Guid.TryParse(id, out var requestId))
                return BadRequest(new { success = false, message = "Invalid request ID" });

            var userRequest = await _context.UserRequests
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
                return NotFound(new { success = false, message = "Request not found" });

            return Ok(new { success = true, data = userRequest });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving request by ID");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateRequestStatus(string id, [FromBody] UpdateRequestStatusDto request)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Check if user has admin rights (you may want to implement proper role checking)
            if (currentUser.Role != "Admin")
                return Forbid("Only administrators can update request status");

            if (!Guid.TryParse(id, out var requestId))
                return BadRequest(new { success = false, message = "Invalid request ID" });

            if (!System.Enum.TryParse<UserRequestStatusEnum>(request.Status, true, out var status))
                return BadRequest(new { success = false, message = "Invalid status" });

            var userRequest = await _context.UserRequests
                .FirstOrDefaultAsync(ur => ur.UserRequestId == requestId);

            if (userRequest == null)
                return NotFound(new { success = false, message = "Request not found" });

            userRequest.Status = status;
            userRequest.ReviewedAt = DateTime.UtcNow;
            userRequest.ReviewedBy = currentUser.Email;
            userRequest.Comments = request.Comments;
            userRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogUserActivity(currentUser.UserId, ActionTypeEnum.Update, 
                $"Updated request {id} status to {request.Status}");

            var responseData = new
            {
                id = userRequest.UserRequestId.ToString(),
                status = userRequest.Status.ToString().ToLower(),
                reviewedAt = userRequest.ReviewedAt,
                reviewedBy = userRequest.ReviewedBy,
                comments = userRequest.Comments
            };

            return Ok(new { success = true, data = responseData, message = "Request status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating request status");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("available-tools")]
    public async Task<IActionResult> GetAvailableTools()
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

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

            return Ok(new { success = true, data = availableTools });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tools");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelRequest(string id)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            if (!Guid.TryParse(id, out var requestId))
                return BadRequest(new { success = false, message = "Invalid request ID" });

            var userRequest = await _context.UserRequests
                .FirstOrDefaultAsync(ur => ur.UserRequestId == requestId && ur.UserId == currentUser.UserId);

            if (userRequest == null)
                return NotFound(new { success = false, message = "Request not found" });

            // Only allow cancellation of pending requests
            if (userRequest.Status != UserRequestStatusEnum.Pending)
                return BadRequest(new { success = false, message = "Only pending requests can be cancelled" });

            userRequest.Status = UserRequestStatusEnum.Cancelled;
            userRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogUserActivity(currentUser.UserId, ActionTypeEnum.ApplicationRemoved, 
                $"Cancelled request {id}");

            return Ok(new { success = true, message = "Request cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling request");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task LogUserActivity(Guid userId, ActionTypeEnum actionType, string description)
    {
        try
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                ActionType = actionType,
                Description = description,
                DeviceInformation = "Web Browser", // You could get this from HttpContext
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity");
        }
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