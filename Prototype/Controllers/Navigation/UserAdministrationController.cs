using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/user-administration")]
public class UserAdministrationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    IUserAccountService userAccountService,
    ILogger<UserAdministrationController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    private readonly IUserAccountService _userAccountService = userAccountService ?? throw new ArgumentNullException(nameof(userAccountService));

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
            // Get target user info for audit logging
            var targetUser = await _userAccountService.GetUserByIdAsync(dto.UserId);
            var targetUserName = targetUser != null ? $"{targetUser.FirstName} {targetUser.LastName}" : "Unknown User";
            
            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                var result = await _userAccountService.UpdateUserAsync(dto);
                
                if (!result.Success)
                {
                    return BadRequestWithMessage(result.Message);
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

            }, ActionTypeEnum.Update, 
               $"Administrator updated user account: {targetUserName} (ID: {dto.UserId}, Username: {dto.Username}, Email: {dto.Email}, Role: {dto.Role}, Active: {dto.IsActive})", 
               "User updated successfully");
        });
    }

    [HttpDelete("delete/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            // Get the target user before deletion for logging
            var targetUser = await _userAccountService.GetUserByIdAsync(userId);
            if (targetUser == null)
                return BadRequestWithMessage("User not found");

            // Prevent deleting own account
            if (currentUser.UserId == userId)
                return BadRequestWithMessage("Cannot delete your own account");

            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                var result = await _userAccountService.DeleteUserAsync(userId);
                
                if (!result.Success)
                {
                    return BadRequestWithMessage(result.Message);
                }

                return SuccessResponse(new { Message = "User deleted successfully" }, "User deleted successfully");

            }, ActionTypeEnum.Delete,
               $"Administrator deleted user account: {targetUser.FirstName} {targetUser.LastName} (ID: {targetUser.UserId}, Username: {targetUser.Username}, Email: {targetUser.Email})",
               "User deleted successfully");
        });
    }

}