using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Settings;

[ApiController]
[Authorize]
[Route("settings/user")]
public class UserSettingsController(
    IEntityCreationFactoryService entityCreationFactory,
    SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return NotFound("User not found.");

        return Ok(new UserSettingsRequestDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return NotFound("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New password does not match confirmation." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var userActivityLog = entityCreationFactory.CreateFromPasswordChange(user, HttpContext);
        var auditLog = entityCreationFactory.CreateFromPasswordChange(user);

        await context.UserActivityLogs.AddAsync(userActivityLog);
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }

    private async Task<UserModel?> GetCurrentUserAsync()
    {
        var idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");

        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }
}