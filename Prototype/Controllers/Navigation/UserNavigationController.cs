using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.Request;
using Prototype.Models;
using Prototype.Services;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("settings/user")]
public class UserNavigationController(
    IAuthenticatedUserAccessor userAccessor,
    ValidationService validationService,
    TransactionService transactionService,
    IUserAccountService userAccountService,
    ICacheService cacheService,
    ICacheInvalidationService cacheInvalidation,
    SentinelContext context,
    ILogger<UserNavigationController> logger)
    : BaseNavigationController(logger, userAccessor, validationService, transactionService)
{
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        // Log the incoming request for debugging
        Logger.LogInformation("Password change request received. ModelState.IsValid: {IsValid}", ModelState.IsValid);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value!.Errors.Select(e => e.ErrorMessage) })
                .ToList();
            
            Logger.LogWarning("Password change validation failed: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            
            return BadRequest(new 
            { 
                success = false, 
                message = "Validation failed", 
                errors = errors 
            });
        }
        
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            Logger.LogInformation("Verifying password for user: {Username}", currentUser.Username);
            Logger.LogInformation("Provided password length: {Length}", dto.CurrentPassword?.Length ?? 0);
            
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentUser.PasswordHash))
            {
                Logger.LogWarning("Password verification failed for user: {Username}", currentUser.Username);
                return new { success = false, message = "Current password is incorrect" };
            }
            
            Logger.LogInformation("Password verification successful for user: {Username}", currentUser.Username);

            return await TransactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Get a fresh copy of the user from database to ensure EF tracking
                var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
                if (trackedUser == null)
                    throw new InvalidOperationException("User not found");

                // Update password
                trackedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                trackedUser.UpdatedAt = DateTime.UtcNow;

                // Save changes to database
                await context.SaveChangesAsync();

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

                context.AuditLogs.Add(auditLog);
                context.UserActivityLogs.Add(activityLog);
                await context.SaveChangesAsync();

                return new { success = true, message = "Password changed successfully" };
            });
        }, "changing password");
    }

    [HttpPut("update-profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserSettingsRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            return await TransactionService!.ExecuteInTransactionAsync(async () =>
            {
                // Get a fresh copy of the user from database to ensure EF tracking
                var trackedUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
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
                await context.SaveChangesAsync();

                // Invalidate user-related caches
                await cacheInvalidation.InvalidateUserCacheAsync(trackedUser.UserId, trackedUser.Email, trackedUser.Username);
                await cacheInvalidation.InvalidateDashboardCacheAsync(); // User info might be shown in dashboard

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

                    context.AuditLogs.Add(auditLog);
                    context.UserActivityLogs.Add(activityLog);
                    await context.SaveChangesAsync();
                }

                var userDto = new UserRequestDto
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
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Check cache first for user profile
            var cacheKey = $"user:profile:full:{currentUser.UserId}";
            var cachedProfile = await cacheService.GetSecureAsync<UserRequestDto>(cacheKey, currentUser.UserId);
            
            if (cachedProfile != null)
            {
                Logger.LogDebug("User profile cache hit for user: {Username}", currentUser.Username);
                return new { success = true, user = cachedProfile, fromCache = true };
            }

            // Get fresh data from database
            var freshUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
            if (freshUser == null)
                return new { success = false, message = "User not found" };

            var userDto = new UserRequestDto
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

            // Cache for 20 minutes (profile data changes occasionally)
            await cacheService.SetSecureAsync(cacheKey, userDto, currentUser.UserId, TimeSpan.FromMinutes(20));
            Logger.LogDebug("User profile cached for user: {Username}", currentUser.Username);

            return new { success = true, user = userDto, fromCache = false };
        }, "retrieving profile");
    }

    [HttpGet("counts")]
    public async Task<IActionResult> GetUserCounts()
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            // Check cache first
            var cacheKey = "users:counts:all";
            var cachedCounts = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedCounts != null)
            {
                Logger.LogDebug("User counts cache hit");
                return new { success = true, data = cachedCounts, fromCache = true };
            }

            var totalVerifiedUsers = await context.Users.CountAsync();
            var totalTemporaryUsers = await context.TemporaryUsers.CountAsync();
            var totalUsers = totalVerifiedUsers + totalTemporaryUsers;

            var countsData = new {
                totalUsers = totalUsers,
                totalVerifiedUsers = totalVerifiedUsers,
                totalTemporaryUsers = totalTemporaryUsers
            };

            // Cache for 10 minutes (user counts change frequently)
            await cacheService.SetAsync(cacheKey, countsData, TimeSpan.FromMinutes(10));
            Logger.LogDebug("User counts cached");

            return new { 
                success = true, 
                data = countsData,
                fromCache = false
            };
        }, "retrieving user counts");
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var (validPage, validPageSize, skip) = ValidatePaginationParameters(page, pageSize);

            // Check cache first - include pagination in cache key
            var cacheKey = $"users:all:page:{validPage}:size:{validPageSize}";
            var cachedUsers = await cacheService.GetAsync<object>(cacheKey);
            
            if (cachedUsers != null)
            {
                Logger.LogDebug("All users cache hit for page: {Page}, pageSize: {PageSize}", validPage, validPageSize);
                return new { success = true, data = cachedUsers, fromCache = true };
            }

            // Get total count for pagination (separate optimized queries)
            var totalVerifiedUsers = await context.Users.CountAsync();
            var totalTempUsers = await context.TemporaryUsers.CountAsync();
            var totalCount = totalVerifiedUsers + totalTempUsers;

            // Create combined query using UNION for database-level pagination
            var verifiedUsersQuery = context.Users
                .Select(user => new UserRequestDto
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
                });

            var tempUsersQuery = context.TemporaryUsers
                .Select(tempUser => new UserRequestDto
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
                });

            // Combine queries with UNION and apply database-level pagination
            var paginatedUsers = await verifiedUsersQuery
                .Union(tempUsersQuery)
                .OrderByDescending(u => u.CreatedAt)
                .Skip(skip)
                .Take(validPageSize)
                .ToListAsync();

            var result = CreatePaginatedResponse(paginatedUsers, validPage, validPageSize, totalCount);
            
            // Cache for 8 minutes (user lists change frequently with new registrations)
            await cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(8));
            Logger.LogDebug("All users cached for page: {Page}, pageSize: {PageSize}", validPage, validPageSize);
            
            return new { success = true, data = result, fromCache = false };
        }, "retrieving all users");
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            var result = await userAccountService.UpdateUserAsync(dto);
            
            if (!result.Success)
            {
                return new { success = false, message = result.Message };
            }
            
            // Invalidate user list caches (UserAccountService already handles user-specific cache)
            await cacheService.RemoveByPatternAsync("users:all:*");
            await cacheService.RemoveAsync("users:counts:all");
            
            // Log the user modification activity for the current user (administrator)
            var targetUser = await userAccountService.GetUserByIdAsync(dto.UserId);
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
                context.AuditLogs.Add(auditLog);
                context.UserActivityLogs.Add(activityLog);
                await context.SaveChangesAsync();
            }

            // Return updated user data
            var updatedUser = await userAccountService.GetUserByIdAsync(dto.UserId);
            if (updatedUser != null)
            {
                var userDto = new UserRequestDto
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

    [HttpDelete("delete/{userId:guid}")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Get the target user before deletion for logging
            var targetUser = await userAccountService.GetUserByIdAsync(userId);
            if (targetUser == null)
                return new { success = false, message = "User not found" };

            // Prevent deleting own account
            if (currentUser.UserId == userId)
                return new { success = false, message = "Cannot delete your own account" };

            var result = await userAccountService.DeleteUserAsync(userId);
            
            if (!result.Success)
            {
                return new { success = false, message = result.Message };
            }
            
            // Invalidate user list caches (UserAccountService already handles user-specific cache)
            await cacheService.RemoveByPatternAsync("users:all:*");
            await cacheService.RemoveAsync("users:counts:all");

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
            context.AuditLogs.Add(auditLog);
            context.UserActivityLogs.Add(activityLog);
            await context.SaveChangesAsync();

            return new { success = true, message = "User deleted successfully" };
        }, "deleting user");
    }

    [HttpPut("update-temporary")]
    public async Task<IActionResult> UpdateTemporaryUser([FromBody] UpdateTemporaryUserRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            var tempUser = await context.TemporaryUsers.FindAsync(dto.TemporaryUserId);
            if (tempUser == null)
                return new { success = false, message = "Temporary user not found" };

            // Store original values for audit logging
            var originalFirstName = tempUser.FirstName;
            var originalLastName = tempUser.LastName;
            var originalEmail = tempUser.Email;
            var originalUsername = tempUser.Username;
            var originalPhoneNumber = tempUser.PhoneNumber;

            // Update the temporary user
            tempUser.FirstName = dto.FirstName;
            tempUser.LastName = dto.LastName;
            tempUser.Email = dto.Email;
            tempUser.Username = dto.Username;
            tempUser.PhoneNumber = dto.PhoneNumber ?? tempUser.PhoneNumber;

            await context.SaveChangesAsync();

            // Create change details for audit log
            var changeDetails = new List<string>();
            if (originalFirstName != dto.FirstName)
                changeDetails.Add($"First Name: {originalFirstName} → {dto.FirstName}");
            if (originalLastName != dto.LastName)
                changeDetails.Add($"Last Name: {originalLastName} → {dto.LastName}");
            if (originalEmail != dto.Email)
                changeDetails.Add($"Email: {originalEmail} → {dto.Email}");
            if (originalUsername != dto.Username)
                changeDetails.Add($"Username: {originalUsername} → {dto.Username}");
            if (originalPhoneNumber != dto.PhoneNumber)
                changeDetails.Add($"Phone: {originalPhoneNumber} → {dto.PhoneNumber}");

            if (changeDetails.Any())
            {
                var metadata = $"Administrator updated temporary user: {dto.FirstName} {dto.LastName} (ID: {dto.TemporaryUserId}) - {string.Join(", ", changeDetails)}";
                
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
                var activityLog = new UserActivityLogModel
                {
                    UserActivityLogId = Guid.NewGuid(),
                    UserId = currentUser.UserId,
                    User = null,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                    ActionType = Enum.ActionTypeEnum.Update,
                    Description = $"Administrator modified temporary user: {dto.FirstName} {dto.LastName} (Username: {dto.Username})",
                    Timestamp = DateTime.UtcNow
                };

                // Add logs to database context and save
                context.AuditLogs.Add(auditLog);
                context.UserActivityLogs.Add(activityLog);
                await context.SaveChangesAsync();
            }

            return new { success = true, message = "Temporary user updated successfully", user = new {
                temporaryUserId = tempUser.TemporaryUserId,
                firstName = tempUser.FirstName,
                lastName = tempUser.LastName,
                username = tempUser.Username,
                email = tempUser.Email,
                phoneNumber = tempUser.PhoneNumber,
                createdAt = tempUser.CreatedAt
            }};
        }, "updating temporary user");
    }

    [HttpDelete("delete-temporary-user/{temporaryUserId:guid}")]
    public async Task<IActionResult> DeleteTemporaryUser(Guid temporaryUserId)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await UserAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            var tempUser = await context.TemporaryUsers.FindAsync(temporaryUserId);
            if (tempUser == null)
            {
                Logger.LogWarning("Attempted to delete temporary user {TemporaryUserId} but user was not found in TemporaryUsers table", temporaryUserId);
                return new { success = false, message = $"Temporary user with ID {temporaryUserId} not found in TemporaryUsers table" };
            }

            // Store user details for audit logging before deletion
            var deletedUserInfo = $"{tempUser.FirstName} {tempUser.LastName} (ID: {tempUser.TemporaryUserId}, Username: {tempUser.Username}, Email: {tempUser.Email})";

            // Remove the temporary user
            context.TemporaryUsers.Remove(tempUser);
            await context.SaveChangesAsync();

            // Create audit log for the administrator who deleted the temporary user
            var metadata = $"Administrator deleted temporary user account: {deletedUserInfo}";
            
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                ActionType = Enum.ActionTypeEnum.Delete,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            // Create user activity log for the administrator
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = currentUser.UserId,
                User = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
                ActionType = Enum.ActionTypeEnum.Delete,
                Description = $"Administrator deleted temporary user account: {tempUser.FirstName} {tempUser.LastName} (Username: {tempUser.Username})",
                Timestamp = DateTime.UtcNow
            };

            // Add logs to the database context and save
            context.AuditLogs.Add(auditLog);
            context.UserActivityLogs.Add(activityLog);
            await context.SaveChangesAsync();

            return new { success = true, message = "Temporary user deleted successfully" };
        }, "deleting temporary user");
    }
}