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

[Route("settings/user/admin")]
public class UserAdministrationController : BaseApiController
{
    private readonly IUserAccountService _userAccountService;

    public UserAdministrationController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        IUserAccountService userAccountService,
        ILogger<UserAdministrationController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
        _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetUserCounts()
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (Context == null)
                throw new InvalidOperationException("Database context is not available");

            var totalVerifiedUsers = await Context.Users.CountAsync();
            var totalTemporaryUsers = await Context.TemporaryUsers.CountAsync();
            var totalUsers = totalVerifiedUsers + totalTemporaryUsers;

            var result = new {
                TotalUsers = totalUsers,
                TotalVerifiedUsers = totalVerifiedUsers,
                TotalTemporaryUsers = totalTemporaryUsers
            };

            return SuccessResponse(result, "User counts retrieved successfully");
        });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (Context == null)
                throw new InvalidOperationException("Database context is not available");

            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            // Get verified users
            var users = await Context.Users
                .Select(user => new UserDto
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    Role = user.Role,
                    LastLogin = user.LastLogin,
                    CreatedAt = user.CreatedAt,
                    IsTemporary = false
                })
                .ToListAsync();

            // Get temporary (unverified) users
            var tempUsers = await Context.TemporaryUsers
                .Select(tempUser => new UserDto
                {
                    UserId = tempUser.TemporaryUserId,
                    FirstName = tempUser.FirstName,
                    LastName = tempUser.LastName,
                    Username = tempUser.Username,
                    Email = tempUser.Email,
                    PhoneNumber = tempUser.PhoneNumber,
                    IsActive = false, // Temporary users are inactive until verified
                    Role = "User", // Default role for temporary users
                    LastLogin = null, // Temporary users haven't logged in
                    CreatedAt = tempUser.CreatedAt,
                    IsTemporary = true
                })
                .ToListAsync();

            // Combine and sort all users by creation date
            var allUsers = users.Concat(tempUsers)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            var totalCount = allUsers.Count;
            var paginatedUsers = allUsers
                .Skip(skip)
                .Take(validPageSize)
                .ToList();

            var result = CreatePaginatedResponse(paginatedUsers, validPage, validPageSize, totalCount);
            return SuccessResponse(result, "Users retrieved successfully");
        });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var result = await _userAccountService.UpdateUserAsync(dto);
                
                if (!result.Success)
                {
                    return BadRequestWithMessage(result.Message);
                }
                
                // Log the user modification activity for the current user (administrator)
                var targetUser = await _userAccountService.GetUserByIdAsync(dto.UserId);
                if (targetUser != null)
                {
                    await LogAuditActivityAsync(currentUser.UserId, ActionTypeEnum.Update,
                        $"Administrator updated user account: {targetUser.FirstName} {targetUser.LastName} (ID: {targetUser.UserId}, Username: {dto.Username}, Email: {dto.Email}, Role: {dto.Role}, Active: {dto.IsActive})",
                        $"Administrator modified user account: {targetUser.FirstName} {targetUser.LastName} (Username: {dto.Username})");
                }

                // Return updated user data
                var updatedUser = await _userAccountService.GetUserByIdAsync(dto.UserId);
                if (updatedUser != null)
                {
                    var userDto = new UserDto
                    {
                        UserId = updatedUser.UserId,
                        FirstName = updatedUser.FirstName,
                        LastName = updatedUser.LastName,
                        Username = updatedUser.Username,
                        Email = updatedUser.Email,
                        PhoneNumber = updatedUser.PhoneNumber,
                        IsActive = updatedUser.IsActive,
                        Role = updatedUser.Role,
                        LastLogin = updatedUser.LastLogin,
                        CreatedAt = updatedUser.CreatedAt
                    };

                    return SuccessResponse(new { Message = result.Message, User = userDto }, result.Message);
                }

                return SuccessResponse(new { Message = result.Message }, result.Message);

            }, "updating user");
        });
    }

    [HttpDelete("delete/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                // Get the target user before deletion for logging
                var targetUser = await _userAccountService.GetUserByIdAsync(userId);
                if (targetUser == null)
                    return BadRequestWithMessage("User not found");

                // Prevent deleting own account
                if (currentUser.UserId == userId)
                    return BadRequestWithMessage("Cannot delete your own account");

                var result = await _userAccountService.DeleteUserAsync(userId);
                
                if (!result.Success)
                {
                    return BadRequestWithMessage(result.Message);
                }

                // Log the user deletion activity for the current user (administrator)
                await LogAuditActivityAsync(currentUser.UserId, ActionTypeEnum.Delete,
                    $"Administrator deleted user account: {targetUser.FirstName} {targetUser.LastName} (ID: {targetUser.UserId}, Username: {targetUser.Username}, Email: {targetUser.Email})",
                    $"Administrator deleted user account: {targetUser.FirstName} {targetUser.LastName} (Username: {targetUser.Username})");

                return SuccessResponse(new { Message = "User deleted successfully" }, "User deleted successfully");

            }, "deleting user");
        });
    }

    private async Task LogAuditActivityAsync(Guid userId, ActionTypeEnum actionType, string auditMetadata, string activityDescription)
    {
        if (Context == null)
            return;

        // Create audit log for the administrator who made the change
        var auditLog = new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = userId,
            User = null,
            ActionType = actionType,
            Metadata = auditMetadata,
            CreatedAt = DateTime.UtcNow
        };

        // Create user activity log for the administrator
        var activityLog = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = userId,
            User = null,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
            ActionType = actionType,
            Description = activityDescription,
            Timestamp = DateTime.UtcNow
        };

        // Add logs to database context and save
        Context.AuditLogs.Add(auditLog);
        Context.UserActivityLogs.Add(activityLog);
        await Context.SaveChangesAsync();
    }
}