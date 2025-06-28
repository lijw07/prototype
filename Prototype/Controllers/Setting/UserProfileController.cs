using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Controllers.Navigation;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Setting;

[Route("settings/user-profile")]
public class UserProfileController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    ILogger<UserProfileController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .Select(x => new { Field = x.Key, Errors = x.Value!.Errors.Select(e => e.ErrorMessage) })
                .ToList();
            
            Logger.LogWarning("Password change validation failed: {Errors}", System.Text.Json.JsonSerializer.Serialize(errors));
            
            return BadRequestWithMessage("Validation failed");
        }

        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                Logger.LogInformation("Verifying password for user: {Username}", currentUser.Username);
                
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentUser.PasswordHash))
                {
                    Logger.LogWarning("Password verification failed for user: {Username}", currentUser.Username);
                    return BadRequestWithMessage("Current password is incorrect");
                }
                
                Logger.LogInformation("Password verification successful for user: {Username}", currentUser.Username);

                return await ExecuteInTransactionWithAuditAsync(async user =>
                {
                    if (Context == null)
                        throw new InvalidOperationException("Database context is not available");

                    // Get a fresh copy of the user from database to ensure EF tracking
                    var trackedUser = await Context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                    if (trackedUser == null)
                        throw new InvalidOperationException("User not found");

                    // Update password
                    trackedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                    trackedUser.UpdatedAt = DateTime.UtcNow;

                    return SuccessResponse<object>(null, "Password changed successfully");

                }, ActionTypeEnum.ChangePassword, "User changed their password", "Password changed successfully");

            }, "changing password");
        });
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserSettingsRequestDto dto)
    {
        return await EnsureUserAuthenticatedAsync(async currentUser =>
        {
            return await ExecuteInTransactionWithAuditAsync(async user =>
            {
                if (Context == null)
                    throw new InvalidOperationException("Database context is not available");

                // Get a fresh copy of the user from database to ensure EF tracking
                var trackedUser = await Context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
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

                // Create audit metadata for profile changes
                var changeDetails = new List<string>();
                if (oldFirstName != dto.FirstName)
                    changeDetails.Add($"First Name: {oldFirstName} → {dto.FirstName}");
                if (oldLastName != dto.LastName)
                    changeDetails.Add($"Last Name: {oldLastName} → {dto.LastName}");
                if (oldEmail != dto.Email)
                    changeDetails.Add($"Email: {oldEmail} → {dto.Email}");

                var metadata = changeDetails.Any() 
                    ? $"User updated profile - {string.Join(", ", changeDetails)}"
                    : "User updated profile - no changes detected";

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

                return SuccessResponse(userDto, "Profile updated successfully");

            }, ActionTypeEnum.Update, "Profile information updated", "Profile updated successfully");
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        return await EnsureUserAuthenticatedAsync<IActionResult>(async currentUser =>
        {
            if (Context == null)
                throw new InvalidOperationException("Database context is not available");

            // Get fresh data from database to avoid cached values
            var freshUser = await Context.Users.FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);
            if (freshUser == null)
                return BadRequestWithMessage(new { success = false, message = "User not found" });

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

            return SuccessResponse(userDto, "Profile retrieved successfully");
        });
    }
}