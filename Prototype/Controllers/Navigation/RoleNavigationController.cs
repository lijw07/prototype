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

[Route("settings/roles")]
public class RoleNavigationController : BaseApiController
{
    private readonly IUserRoleService _userRoleService;

    public RoleNavigationController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        IUserRoleService userRoleService,
        ILogger<RoleNavigationController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
        _userRoleService = userRoleService ?? throw new ArgumentNullException(nameof(userRoleService));
    }
    [HttpGet]
    public async Task<IActionResult> GetAllRoles([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            if (Context == null)
                throw new InvalidOperationException("Database context is not available");

            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            var totalCount = await Context.UserRoles.CountAsync();
            var roles = await Context.UserRoles
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
            return SuccessResponse(result, "Roles retrieved successfully");
        });
    }

    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetRoleById(Guid roleId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            var role = await _userRoleService.GetRoleByIdAsync(roleId);
            
            if (role == null)
                return BadRequestWithMessage("Role not found");

            var roleDto = new RoleDto
            {
                UserRoleId = role.UserRoleId,
                Role = role.Role,
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy
            };

            return SuccessResponse(new { Role = roleDto }, "Role retrieved successfully");
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Check if role already exists using the same context
                var roleExists = await Context.UserRoles
                    .AnyAsync(r => r.Role.ToLower() == dto.RoleName.ToLower());
                if (roleExists)
                    return BadRequestWithMessage("A role with this name already exists");

                // Create role directly in the controller's context (within transaction)
                var role = new UserRoleModel
                {
                    UserRoleId = Guid.NewGuid(),
                    Role = dto.RoleName,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.Username
                };
                Context.UserRoles.Add(role);
                
                Logger.LogInformation("Role created in transaction, now creating logs for role: {RoleName}", dto.RoleName);
                
                var roleDto = new RoleDto
                {
                    UserRoleId = role.UserRoleId,
                    Role = role.Role,
                    CreatedAt = role.CreatedAt,
                    CreatedBy = role.CreatedBy
                };

                Logger.LogInformation("Role '{RoleName}' created by user {Username}", dto.RoleName, user.Username);
                return SuccessResponse(new { Message = "Role created successfully", Role = roleDto }, "Role created successfully");

            }, ActionTypeEnum.RoleCreated, $"User created role: {dto.RoleName}", "Role created successfully");
        });
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] CreateRoleDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            // Get role name first to use in audit message
            var existingRoleForAudit = await Context!.UserRoles
                .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
            var oldRoleName = existingRoleForAudit?.Role ?? "Unknown";

            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Check if role exists using the same context
                var existingRole = await Context.UserRoles
                    .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
                if (existingRole == null)
                    return BadRequestWithMessage("Role not found");

                // Check if another role with the same name exists using the same context
                var roleWithSameName = await Context.UserRoles
                    .AnyAsync(r => r.Role.ToLower() == dto.RoleName.ToLower());
                if (roleWithSameName && !existingRole.Role.Equals(dto.RoleName, StringComparison.OrdinalIgnoreCase))
                    return BadRequestWithMessage("A role with this name already exists");

                // Update role directly in the controller's context (within transaction)
                existingRole.Role = dto.RoleName;
                
                Logger.LogInformation("Role updated in transaction, now creating logs for role update: {OldName} -> {NewName}", oldRoleName, dto.RoleName);

                var roleDto = new RoleDto
                {
                    UserRoleId = existingRole.UserRoleId,
                    Role = existingRole.Role,
                    CreatedAt = existingRole.CreatedAt,
                    CreatedBy = existingRole.CreatedBy
                };

                Logger.LogInformation("Role '{OldRoleName}' updated to '{NewRoleName}' by user {Username}", oldRoleName, dto.RoleName, user.Username);
                return SuccessResponse(new { Message = "Role updated successfully", Role = roleDto }, "Role updated successfully");

            }, ActionTypeEnum.RoleUpdated, $"User updated role from '{oldRoleName}' to '{dto.RoleName}'", "Role updated successfully");
        });
    }

    [HttpGet("{roleId}/deletion-constraints")]
    public async Task<IActionResult> GetRoleDeletionConstraints(Guid roleId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Get role details
                var role = await Context.UserRoles
                    .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
                if (role == null)
                    return BadRequestWithMessage("Role not found");

                var roleName = role.Role;
                
                // Check if role is assigned to any users
                var usersWithRole = await Context.Users
                    .Where(u => u.Role.ToLower() == roleName.ToLower())
                    .CountAsync();
                
                // Check if role is assigned to any temporary users (specifically for "User" role)
                var tempUsersCount = await Context.TemporaryUsers.CountAsync();
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
                
                var result = new { 
                    CanDelete = canDelete,
                    UsersCount = usersWithRole,
                    TemporaryUsersCount = blockedByTempUsers ? tempUsersCount : 0,
                    ConstraintMessage = constraintMessage,
                    RoleName = roleName
                };

                return SuccessResponse(result, "Role deletion constraints retrieved successfully");

            }, "checking role deletion constraints");
        });
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Get role details before deletion for logging using the same context
                var roleToDelete = await Context.UserRoles
                    .FirstOrDefaultAsync(r => r.UserRoleId == roleId);
                if (roleToDelete == null)
                    return BadRequestWithMessage("Role not found");

                var roleName = roleToDelete.Role;
                
                // Check if role is assigned to any users before deletion
                var usersWithRole = await Context.Users
                    .Where(u => u.Role.ToLower() == roleName.ToLower())
                    .CountAsync();
                
                if (usersWithRole > 0)
                {
                    Logger.LogWarning("Cannot delete role '{RoleName}' - it is assigned to {UserCount} user(s)", roleName, usersWithRole);
                    return BadRequestWithMessage($"Cannot delete role '{roleName}' - it is assigned to {usersWithRole} user(s). Please reassign users to different roles before deletion.");
                }
                
                // Check if role is assigned to any temporary users
                var tempUsersCount = await Context.TemporaryUsers.CountAsync();
                if (tempUsersCount > 0 && roleName.ToLower() == "user")
                {
                    Logger.LogWarning("Cannot delete 'User' role - there are {TempUserCount} temporary user(s) that will be assigned this role upon activation", tempUsersCount);
                    return BadRequestWithMessage($"Cannot delete 'User' role - there are {tempUsersCount} temporary user(s) that will be assigned this role upon activation.");
                }
                
                Logger.LogInformation("Role '{RoleName}' passed constraint checks - no users assigned", roleName);
                
                // Delete role directly in the controller's context (within transaction)
                Context.UserRoles.Remove(roleToDelete);
                
                Logger.LogInformation("Role deleted in transaction, now creating logs for role deletion: {RoleName}", roleName);

                Logger.LogInformation("Role '{RoleName}' deleted by user {Username}", roleName, user.Username);
                return SuccessResponse(new { Message = "Role deleted successfully" }, "Role deleted successfully");

            }, ActionTypeEnum.RoleDeleted, $"User deleted role: {roleId}", "Role deleted successfully");
        });
    }
}