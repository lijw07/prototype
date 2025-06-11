using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[ApiController]
[Authorize]
[Route("settings/user")]
public class UserSettingsController(
    IEntityCreationFactoryService entityCreationFactory,
    IAuthenticatedUserAccessor userAccessor,
    SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
    {
        var user = await userAccessor.GetUserAsync(User);
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
        var user = await userAccessor.GetUserAsync(User);
        if (user is null)
            return NotFound("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.NewPassword != request.ConfirmNewPassword)
            return BadRequest(new { message = "New password does not match confirmation." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ChangePassword, HttpContext);
        var auditLog = entityCreationFactory.CreateFromPasswordChange(user);

        await context.UserActivityLogs.AddAsync(userActivityLog);
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }
}