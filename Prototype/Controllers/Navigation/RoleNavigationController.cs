using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("settings/roles")]
public class RoleNavigationController(
    IAuthenticatedUserAccessor userAccessor,
    ValidationService validationService,
    TransactionService transactionService,
    IUserRoleService userRoleService,
    SentinelContext context,
    ILogger<RoleNavigationController> logger)
    : BaseNavigationController(logger, userAccessor, validationService, transactionService)
{
    [HttpGet]
    public async Task<IActionResult> GetAllRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var totalCount = await context.UserRoles.CountAsync();
            var roles = await context.UserRoles
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();
            
            var roleDtos = roles.Select(role => new RoleRequestDto
            {
                UserRoleId = role.UserRoleId,
                RoleName = role.RoleName,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            }).ToList();

            var result = CreatePaginatedResponse(roleDtos, validPage, validPageSize, totalCount);
            return new { success = true, data = result };
        }, "retrieving all roles");
    }

    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetRoleById(Guid roleId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var role = await userRoleService.GetRoleByIdAsync(roleId);
            
            if (role == null)
                return new { success = false, message = "Role not found" };

            var roleDto = new RoleRequestDto
            {
                UserRoleId = role.UserRoleId,
                RoleName = role.RoleName,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            };

            return new { success = true, role = roleDto };
        }, "retrieving role");
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleRequestDto requestDto)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check if role already exists using the same context
            var roleExists = await context.UserRoles
                .AnyAsync(r => r.RoleName.ToLower() == requestDto.RoleName.ToLower());
            if (roleExists)
                return new { success = false, message = "A role with this name already exists" };

            // Create role directly in the controller's context (within transaction)
            var role = new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                RoleName = requestDto.RoleName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.Username
            };
            context.UserRoles.Add(role);
            
            Logger.LogInformation("Role created in transaction, now creating logs for role: {RoleName}", requestDto.RoleName);
            
            // Create audit logs in the same transaction using the same context
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                ActionType = ActionTypeEnum.RoleCreated,
                Description = $"User created role: {requestDto.RoleName}",
                Timestamp = DateTime.UtcNow
            };
            context.UserActivityLogs.Add(activityLog);
            Logger.LogInformation("Added UserActivityLog for role creation");
            
            // Create audit log entry
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.RoleCreated,
                Metadata = $"Role '{requestDto.RoleName}' created by user {currentUser.Username}. Role ID: {role.UserRoleId}",
                UserId = currentUser.UserId,
                User = null,
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);
            Logger.LogInformation("Added AuditLog for role creation");
            
            // All changes will be committed together by the transaction service
            Logger.LogInformation("Transaction will commit role and logs together for: {RoleName}", requestDto.RoleName);
            
            var roleDto = new RoleRequestDto
            {
                UserRoleId = role.UserRoleId,
                RoleName = role.RoleName,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            };

            Logger.LogInformation("Role '{RoleName}' created by user {Username}", requestDto.RoleName, currentUser.Username);
            return new { success = true, message = "Role created successfully", role = roleDto };
        });
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] RoleRequestDto requestDto)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check if role exists using the same context
            var existingRole = await context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (existingRole == null)
                return new { success = false, message = "Role not found" };

            // Check if another role with the same name exists using the same context
            var roleWithSameName = await context.UserRoles
                .AnyAsync(r => r.RoleName.ToLower() == requestDto.RoleName.ToLower());
            if (roleWithSameName && !existingRole.RoleName.Equals(requestDto.RoleName, StringComparison.OrdinalIgnoreCase))
                return new { success = false, message = "A role with this name already exists" };

            var oldRoleName = existingRole.RoleName;
            
            // Update role directly in the controller's context (within transaction)
            existingRole.RoleName = requestDto.RoleName;
            
            Logger.LogInformation("Role updated in transaction, now creating logs for role update: {OldName} -> {NewName}", oldRoleName, requestDto.RoleName);
            
            // Create audit logs in the same transaction using the same context
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                ActionType = ActionTypeEnum.RoleUpdated,
                Description = $"User updated role from '{oldRoleName}' to '{requestDto.RoleName}'",
                Timestamp = DateTime.UtcNow
            };
            context.UserActivityLogs.Add(activityLog);
            Logger.LogInformation("Added UserActivityLog for role update");
            
            // Create audit log entry
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.RoleUpdated,
                Metadata = $"Role updated from '{oldRoleName}' to '{requestDto.RoleName}' by user {currentUser.Username}. Role ID: {roleId}",
                UserId = currentUser.UserId,
                User = null,
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);
            Logger.LogInformation("Added AuditLog for role update");
            
            // All changes will be committed together by the transaction service
            Logger.LogInformation("Transaction will commit role update and logs together for: {OldName} -> {NewName}", oldRoleName, requestDto.RoleName);

            var roleDto = new RoleRequestDto
            {
                UserRoleId = existingRole.UserRoleId,
                RoleName = existingRole.RoleName,
                CreatedAt = existingRole.CreatedAt,
                CreatedBy = existingRole.CreatedBy
            };

            Logger.LogInformation("Role '{OldRoleName}' updated to '{NewRoleName}' by user {Username}", oldRoleName, requestDto.RoleName, currentUser.Username);
            return new { success = true, message = "Role updated successfully", role = roleDto };
        });
    }

    [HttpGet("{roleId}/deletion-constraints")]
    public async Task<IActionResult> GetRoleDeletionConstraints(Guid roleId)
    {
        try
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Get role details
            var role = await context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (role == null)
                return NotFound(new { success = false, message = "Role not found" });

            var roleName = role.RoleName;
            
            // Check if role is assigned to any users
            var usersWithRole = await context.Users
                .Where(u => u.Role.ToLower() == roleName.ToLower())
                .CountAsync();
            
            // Check if role is assigned to any temporary users (specifically for "User" role)
            var tempUsersCount = await context.TemporaryUsers.CountAsync();
            var blockedByTempUsers = tempUsersCount > 0 && roleName.ToLower() == "user";
            
            var canDelete = usersWithRole == 0 && !blockedByTempUsers;
            
            string constraintMessage = "";
            if (usersWithRole > 0)
            {
                constraintMessage = $"Cannot delete role '{roleName}' - it is assigned to {usersWithRole} user(s). Please reassign users to different roles before deletion.";
            }
            else if (blockedByTempUsers)
            {
                constraintMessage = $"Cannot delete 'User' role - there are {tempUsersCount} temporary user(s) that will be assigned this role upon activation.";
            }
            
            return Ok(new { 
                success = true, 
                canDelete = canDelete,
                usersCount = usersWithRole,
                temporaryUsersCount = blockedByTempUsers ? tempUsersCount : 0,
                constraintMessage = constraintMessage,
                roleName = roleName
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking role deletion constraints for role {RoleId}", roleId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Get role details before deletion for logging using the same context
            var roleToDelete = await context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (roleToDelete == null)
                return new { success = false, message = "Role not found" };

            var roleName = roleToDelete.RoleName;
            
            // Check if role is assigned to any users before deletion
            var usersWithRole = await context.Users
                .Where(u => u.Role.ToLower() == roleName.ToLower())
                .CountAsync();
            
            if (usersWithRole > 0)
            {
                Logger.LogWarning("Cannot delete role '{RoleName}' - it is assigned to {UserCount} user(s)", roleName, usersWithRole);
                return new { success = false, message = $"Cannot delete role '{roleName}' - it is assigned to {usersWithRole} user(s). Please reassign users to different roles before deletion." };
            }
            
            // Check if role is assigned to any temporary users
            var tempUsersCount = await context.TemporaryUsers.CountAsync();
            if (tempUsersCount > 0 && roleName.ToLower() == "user")
            {
                Logger.LogWarning("Cannot delete 'User' role - there are {TempUserCount} temporary user(s) that will be assigned this role upon activation", tempUsersCount);
                return new { success = false, message = $"Cannot delete 'User' role - there are {tempUsersCount} temporary user(s) that will be assigned this role upon activation." };
            }
            
            Logger.LogInformation("Role '{RoleName}' passed constraint checks - no users assigned", roleName);
            
            // Delete role directly in the controller's context (within transaction)
            context.UserRoles.Remove(roleToDelete);
            
            Logger.LogInformation("Role deleted in transaction, now creating logs for role deletion: {RoleName}", roleName);
            
            // Create audit logs in the same transaction using the same context
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                ActionType = ActionTypeEnum.RoleDeleted,
                Description = $"User deleted role: {roleName}",
                Timestamp = DateTime.UtcNow
            };
            context.UserActivityLogs.Add(activityLog);
            Logger.LogInformation("Added UserActivityLog for role deletion");
            
            // Create audit log entry
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.RoleDeleted,
                Metadata = $"Role '{roleName}' deleted by user {currentUser.Username}. Role ID: {roleId}",
                UserId = currentUser.UserId,
                User = null,
                CreatedAt = DateTime.UtcNow
            };
            context.AuditLogs.Add(auditLog);
            Logger.LogInformation("Added AuditLog for role deletion");
            
            // All changes will be committed together by the transaction service
            Logger.LogInformation("Transaction will commit role deletion and logs together for: {RoleName}", roleName);

            Logger.LogInformation("Role '{RoleName}' deleted by user {Username}", roleName, currentUser.Username);
            return new { success = true, message = "Role deleted successfully" };
        });
    }
}