using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[Route("settings/user")]
public class UserSettingsController : BaseSettingsController
{
    public UserSettingsController(
        IAuthenticatedUserAccessor userAccessor,
        ValidationService validationService,
        TransactionService transactionService,
        ILogger<UserSettingsController> logger)
        : base(logger, userAccessor, validationService, transactionService)
    {
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        return await ExecuteWithErrorHandlingAsync<object>(async () =>
        {
            var currentUser = await _userAccessor!.GetCurrentUserAsync(User);
            if (currentUser == null)
                return new { success = false, message = "User not authenticated" };

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, currentUser.PasswordHash))
            {
                return new { success = false, message = "Current password is incorrect" };
            }

            return await _transactionService!.ExecuteInTransactionAsync(() =>
            {
                // Update password
                currentUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                currentUser.UpdatedAt = DateTime.UtcNow;

                // Note: TransactionService handles the context, so we return the result
                return Task.FromResult(new { success = true, message = "Password changed successfully" });
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

            return await _transactionService!.ExecuteInTransactionAsync(() =>
            {
                // Update user profile
                currentUser.FirstName = dto.FirstName;
                currentUser.LastName = dto.LastName;
                currentUser.Email = dto.Email;
                currentUser.UpdatedAt = DateTime.UtcNow;

                var userDto = new UserDto
                {
                    UserId = currentUser.UserId,
                    FirstName = currentUser.FirstName,
                    LastName = currentUser.LastName,
                    Username = currentUser.Username,
                    Email = currentUser.Email,
                    PhoneNumber = currentUser.PhoneNumber
                };

                return Task.FromResult(new { success = true, message = "Profile updated successfully", user = userDto });
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

            var userDto = new UserDto
            {
                UserId = currentUser.UserId,
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                Username = currentUser.Username,
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber
            };

            return new { success = true, user = userDto };
        }, "retrieving profile");
    }
}