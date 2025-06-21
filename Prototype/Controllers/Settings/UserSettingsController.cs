using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using Prototype.Data;
using System.Linq;

namespace Prototype.Controllers.Settings;

[Route("settings/user")]
public class UserSettingsController : BaseSettingsController
{
    private readonly IUserAccountService _userAccountService;
    private readonly SentinelContext _context;

    public UserSettingsController(
        IAuthenticatedUserAccessor userAccessor,
        ValidationService validationService,
        TransactionService transactionService,
        IUserAccountService userAccountService,
        SentinelContext context,
        ILogger<UserSettingsController> logger)
        : base(logger, userAccessor, validationService, transactionService)
    {
        _userAccountService = userAccountService;
        _context = context;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        // Log the incoming request for debugging
        _logger.LogInformation("Password change request received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value!.Errors.Select(e => e.ErrorMessage) })
                .ToList();
            
            _logger.LogWarning("Password change validation failed: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            
            return BadRequest(new 
            { 
                success = false, 
                message = "Validation failed", 
                errors = errors 
            });
        }
        
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            _logger.LogInformation("Verifying password for user: {Username}", currentUser.Username);
            _logger.LogInformation("Provided password length: {Length}", dto.CurrentPassword?.Length ?? 0);
            
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentUser.PasswordHash))
            {
                _logger.LogWarning("Password verification failed for user: {Username}", currentUser.Username);
                return new { success = false, message = "Current password is incorrect" };
            }
            
            _logger.LogInformation("Password verification successful for user: {Username}", currentUser.Username);

            return await _transactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Get a fresh copy of the user from database to ensure EF tracking
                var trackedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
                if (trackedUser == null)
                    throw new InvalidOperationException("User not found");

                // Update password
                trackedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                trackedUser.UpdatedAt = DateTime.UtcNow;

                // Save changes to database
                await _context.SaveChangesAsync();

                // Create audit log for password change
                var auditLog = new Models.AuditLogModel
                {
                    AuditLogId = Guid.NewGuid(),
                    UserId = trackedUser.UserId,
                    User = null,
                    ActionType = Enum.ActionTypeEnum.ChangePassword,
                    Metadata = "User changed their password",
                    CreatedAt = DateTime.UtcNow
                };

                var activityLog = new Models.UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = trackedUser.UserId,
                    User = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                    ActionType = Enum.ActionTypeEnum.ChangePassword,
                    Description = "Password changed successfully",
                    Timestamp = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                _context.UserActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                return new { success = true, message = "Password changed successfully" };
            });
        }, "changing password");
    }

    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserSettingsRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            return await _transactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Get a fresh copy of the user from database to ensure EF tracking
                var trackedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
                if (trackedUser == null)
                    throw new InvalidOperationException("User not found");

                // Capture old values for logging
                var oldFirstName = trackedUser.FirstName;
                var oldLastName = trackedUser.LastName;
                var oldEmail = trackedUser.Email;

                // Update user profile
                trackedUser.FirstName = dto.FirstName;
                trackedUser.LastName = dto.LastName;
                trackedUser.Email = dto.Email;
                trackedUser.UpdatedAt = DateTime.UtcNow;

                // Save changes to database
                await _context.SaveChangesAsync();

                // Create audit log for profile update
                var changeDetails = new List<string>();
                if (oldFirstName != dto.FirstName)
                    changeDetails.Add($"First Name: {oldFirstName} → {dto.FirstName}");
                if (oldLastName != dto.LastName)
                    changeDetails.Add($"Last Name: {oldLastName} → {dto.LastName}");
                if (oldEmail != dto.Email)
                    changeDetails.Add($"Email: {oldEmail} → {dto.Email}");

                if (changeDetails.Any())
                {
                    var metadata = $"User updated profile - {string.Join(", ", changeDetails)}";
                    
                    var auditLog = new Models.AuditLogModel
                    {
                        AuditLogId = Guid.NewGuid(),
                        UserId = trackedUser.UserId,
                        User = null,
                        ActionType = Enum.ActionTypeEnum.Update,
                        Metadata = metadata,
                        CreatedAt = DateTime.UtcNow
                    };

                    var activityLog = new Models.UserActivityLogModel
                    {
                        UserActivityLogId = Guid.NewGuid(),
                        UserId = trackedUser.UserId,
                        User = null,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                        ActionType = Enum.ActionTypeEnum.Update,
                        Description = "Profile information updated",
                        Timestamp = DateTime.UtcNow
                    };

                    _context.AuditLogs.Add(auditLog);
                    _context.UserActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();
                }

                var userDto = new UserDto
                {
                    UserId = trackedUser.UserId,
                    FirstName = trackedUser.FirstName,
                    LastName = trackedUser.LastName,
                    Username = trackedUser.Username,
                    Email = trackedUser.Email,
                    PhoneNumber = trackedUser.PhoneNumber,
                    IsActive = trackedUser.IsActive,
                    Role = trackedUser.Role,
                    LastLogin = trackedUser.LastLogin,
                    CreatedAt = trackedUser.CreatedAt
                };

                return new { success = true, message = "Profile updated successfully", user = userDto };
            });
        }, "updating profile");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Get fresh data from database to avoid cached values
            var freshUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
            if (freshUser == null)
                return new { success = false, message = "User not found" };

            var userDto = new UserDto
            {
                UserId = freshUser.UserId,
                FirstName = freshUser.FirstName,
                LastName = freshUser.LastName,
                Username = freshUser.Username,
                Email = freshUser.Email,
                PhoneNumber = freshUser.PhoneNumber,
                IsActive = freshUser.IsActive,
                Role = freshUser.Role,
                LastLogin = freshUser.LastLogin,
                CreatedAt = freshUser.CreatedAt
            };

            return new { success = true, user = userDto };
        }, "retrieving profile");
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var users = await _userAccountService.GetAllUsersAsync();
            
            var userDtos = users.Select(user => new UserDto
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
                CreatedAt = user.CreatedAt
            }).ToList();

            return new { success = true, users = userDtos };
        }, "retrieving all users");
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            var result = await _userAccountService.UpdateUserAsync(dto);
            
            if (!result.Success)
            {
                return new { success = false, message = result.Message };
            }
            
            // Log the user modification activity for the current user (administrator)
            var targetUser = await _userAccountService.GetUserByIdAsync(dto.UserId);
            if (targetUser != null)
            {
                var metadata = $"Administrator updated user account: {targetUser.FirstName} {targetUser.LastName} (ID: {targetUser.UserId}, Username: {dto.Username}, Email: {dto.Email}, Role: {dto.Role}, Active: {dto.IsActive})";
                
                // Create audit log for the administrator who made the change
                var auditLog = new Models.AuditLogModel
                {
                    AuditLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null,
                    ActionType = Enum.ActionTypeEnum.Update,
                    Metadata = metadata,
                    CreatedAt = DateTime.UtcNow
                };

                // Create user activity log for the administrator
                var activityLog = new Models.UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                    ActionType = Enum.ActionTypeEnum.Update,
                    Description = $"Administrator modified user account: {targetUser.FirstName} {targetUser.LastName} (Username: {dto.Username})",
                    Timestamp = DateTime.UtcNow
                };

                // Add logs to database context and save
                _context.AuditLogs.Add(auditLog);
                _context.UserActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();
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

                return new { success = true, message = result.Message, user = userDto };
            }

            return new { success = true, message = result.Message };
        }, "updating user");
    }

    [HttpDelete("delete/{userId}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Get the target user before deletion for logging
            var targetUser = await _userAccountService.GetUserByIdAsync(userId);
            if (targetUser == null)
                return new { success = false, message = "User not found" };

            // Prevent deleting own account
            if (currentUser.UserId == userId)
                return new { success = false, message = "Cannot delete your own account" };

            var result = await _userAccountService.DeleteUserAsync(userId);
            
            if (!result.Success)
            {
                return new { success = false, message = result.Message };
            }

            // Log the user deletion activity for the current user (administrator)
            var metadata = $"Administrator deleted user account: {targetUser.FirstName} {targetUser.LastName} (ID: {targetUser.UserId}, Username: {targetUser.Username}, Email: {targetUser.Email})";
            
            // Create audit log for the administrator who deleted the user
            var auditLog = new Models.AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                ActionType = Enum.ActionTypeEnum.Delete,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            // Create user activity log for the administrator
            var activityLog = new Models.UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                ActionType = Enum.ActionTypeEnum.Delete,
                Description = $"Administrator deleted user account: {targetUser.FirstName} {targetUser.LastName} (Username: {targetUser.Username})",
                Timestamp = DateTime.UtcNow
            };

            // Add logs to the database context and save
            _context.AuditLogs.Add(auditLog);
            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();

            return new { success = true, message = "User deleted successfully" };
        }, "deleting user");
    }

}