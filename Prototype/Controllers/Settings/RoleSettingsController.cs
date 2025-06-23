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

[Route("settings/roles")]
public class RoleSettingsController : BaseSettingsController
{
    private readonly IUserRoleService _userRoleService;
    private readonly SentinelContext _context;

    public RoleSettingsController(
        IAuthenticatedUserAccessor userAccessor,
        ValidationService validationService,
        TransactionService transactionService,
        IUserRoleService userRoleService,
        SentinelContext context,
        ILogger<RoleSettingsController> logger)
        : base(logger, userAccessor, validationService, transactionService)
    {
        _userRoleService = userRoleService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var totalCount = await _context.UserRoles.CountAsync();
            var roles = await _context.UserRoles
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();
            
            var roleDtos = roles.Select(role => new RoleDto
            {
                UserRoleId = role.UserRoleId,
                Role = role.Role,
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
            var role = await _userRoleService.GetRoleByIdAsync(roleId);
            
            if (role == null)
                return new { success = false, message = "Role not found" };

            var roleDto = new RoleDto
            {
                UserRoleId = role.UserRoleId,
                Role = role.Role,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            };

            return new { success = true, role = roleDto };
        }, "retrieving role");
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check if role already exists using the same context
            var roleExists = await _context.UserRoles
                .AnyAsync(r => r.Role.ToLower() == dto.RoleName.ToLower());
            if (roleExists)
                return new { success = false, message = "A role with this name already exists" };

            // Create role directly in the controller's context (within transaction)
            var role = new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                Role = dto.RoleName,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser.Username
            };
            _context.UserRoles.Add(role);
            
            _logger.LogInformation("Role created in transaction, now creating logs for role: {RoleName}", dto.RoleName);
            
            // Create audit logs in the same transaction using the same context
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                ActionType = ActionTypeEnum.RoleCreated,
                Description = $"User created role: {dto.RoleName}",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivityLogs.Add(activityLog);
            _logger.LogInformation("Added UserActivityLog for role creation");
            
            // Create audit log entry
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.RoleCreated,
                Metadata = $"Role '{dto.RoleName}' created by user {currentUser.Username}. Role ID: {role.UserRoleId}",
                UserId = currentUser.UserId,
                User = null,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            _logger.LogInformation("Added AuditLog for role creation");
            
            // All changes will be committed together by the transaction service
            _logger.LogInformation("Transaction will commit role and logs together for: {RoleName}", dto.RoleName);
            
            var roleDto = new RoleDto
            {
                UserRoleId = role.UserRoleId,
                Role = role.Role,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            };

            _logger.LogInformation("Role '{RoleName}' created by user {Username}", dto.RoleName, currentUser.Username);
            return new { success = true, message = "Role created successfully", role = roleDto };
        });
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] CreateRoleDto dto)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check if role exists using the same context
            var existingRole = await _context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (existingRole == null)
                return new { success = false, message = "Role not found" };

            // Check if another role with the same name exists using the same context
            var roleWithSameName = await _context.UserRoles
                .AnyAsync(r => r.Role.ToLower() == dto.RoleName.ToLower());
            if (roleWithSameName && !existingRole.Role.Equals(dto.RoleName, StringComparison.OrdinalIgnoreCase))
                return new { success = false, message = "A role with this name already exists" };

            var oldRoleName = existingRole.Role;
            
            // Update role directly in the controller's context (within transaction)
            existingRole.Role = dto.RoleName;
            
            _logger.LogInformation("Role updated in transaction, now creating logs for role update: {OldName} -> {NewName}", oldRoleName, dto.RoleName);
            
            // Create audit logs in the same transaction using the same context
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString(),
                ActionType = ActionTypeEnum.RoleUpdated,
                Description = $"User updated role from '{oldRoleName}' to '{dto.RoleName}'",
                Timestamp = DateTime.UtcNow
            };
            _context.UserActivityLogs.Add(activityLog);
            _logger.LogInformation("Added UserActivityLog for role update");
            
            // Create audit log entry
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                ActionType = ActionTypeEnum.RoleUpdated,
                Metadata = $"Role updated from '{oldRoleName}' to '{dto.RoleName}' by user {currentUser.Username}. Role ID: {roleId}",
                UserId = currentUser.UserId,
                User = null,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);
            _logger.LogInformation("Added AuditLog for role update");
            
            // All changes will be committed together by the transaction service
            _logger.LogInformation("Transaction will commit role update and logs together for: {OldName} -> {NewName}", oldRoleName, dto.RoleName);

            var roleDto = new RoleDto
            {
                UserRoleId = existingRole.UserRoleId,
                Role = existingRole.Role,
                CreatedAt = existingRole.CreatedAt,
                CreatedBy = existingRole.CreatedBy
            };

            _logger.LogInformation("Role '{OldRoleName}' updated to '{NewRoleName}' by user {Username}", oldRoleName, dto.RoleName, currentUser.Username);
            return new { success = true, message = "Role updated successfully", role = roleDto };
        });
    }

    [HttpGet("{roleId}/deletion-constraints")]
    public async Task<IActionResult> GetRoleDeletionConstraints(Guid roleId)
    {
        try
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            // Get role details
            var role = await _context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (role == null)
                return NotFound(new { success = false, message = "Role not found" });

            var roleName = role.Role;
            
            // Check if role is assigned to any users
            var usersWithRole = await _context.Users
                .Where(u => u.Role.ToLower() == roleName.ToLower())
                .CountAsync();
            
            // Check if role is assigned to any temporary users (specifically for "User" role)
            var tempUsersCount = await _context.TemporaryUsers.CountAsync();
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
            _logger.LogError(ex, "Error checking role deletion constraints for role {RoleId}", roleId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        return await ExecuteInTransactionAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Get role details before deletion for logging using the same context
            var roleToDelete = await _context.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            if (roleToDelete == null)
                return new { success = false, message = "Role not found" };

            var roleName = roleToDelete.Role;
            
            // Check if role is assigned to any users before deletion
            var usersWithRole = await _context.Users
                .Where(u => u.Role.ToLower() == roleName.ToLower())
                .CountAsync();
            
            if (usersWithRole > 0)
            {
                _logger.LogWarning("Cannot delete role '{RoleName}' - it is assigned to {UserCount} user(s)", roleName, usersWithRole);
                return new { success = false, message = $"Cannot delete role '{roleName}' - it is assigned to {usersWithRole} user(s). Please reassign users to different roles before deletion." };
            }
            
            // Check if role is assigned to any temporary users
            var tempUsersCount = await _context.TemporaryUsers.CountAsync();
            if (tempUsersCount > 0 && roleName.ToLower() == "user")
            {
                _logger.LogWarning("Cannot delete 'User' role - there are {TempUserCount} temporary user(s) that will be assigned this role upon activation", tempUsersCount);
                return new { success = false, message = $"Cannot delete 'User' role - there are {tempUsersCount} temporary user(s) that will be assigned this role upon activation." };
            }
            
            _logger.LogInformation("Role '{RoleName}' passed constraint checks - no users assigned", roleName);
            
            // Delete role directly in the controller's context (within transaction)
            _context.UserRoles.Remove(roleToDelete);
            
            _logger.LogInformation("Role deleted in transaction, now creating logs for role deletion: {RoleName}", roleName);
            
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
            _context.UserActivityLogs.Add(activityLog);
            _logger.LogInformation("Added UserActivityLog for role deletion");
            
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
            _context.AuditLogs.Add(auditLog);
            _logger.LogInformation("Added AuditLog for role deletion");
            
            // All changes will be committed together by the transaction service
            _logger.LogInformation("Transaction will commit role deletion and logs together for: {RoleName}", roleName);

            _logger.LogInformation("Role '{RoleName}' deleted by user {Username}", roleName, currentUser.Username);
            return new { success = true, message = "Role deleted successfully" };
        });
    }
}