using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Settings;

[ApiController]
[Authorize]
[Route("settings/user")]
public class UserSettingsController(
    IEntityCreationService entityCreation,
    IAuthenticatedUserAccessor userAccessor,
    IUnitOfWorkService uows) : ControllerBase
{
    
    private UserModel? _user;

    private async Task<UserModel?> GetCurrentUserAsync()
    {
        if (_user == null)
            _user = await userAccessor.GetUserFromTokenAsync(User);
        return _user;
    }
    
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

    [HttpPut]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var user = await GetCurrentUserAsync();
        
        if (user is null)
            return NotFound("User not found.");

        if (!request.NewPassword.Equals(request.ReTypeNewPassword))
            return BadRequest(new { message = "New password does not match." });
        
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.Now;

        var userActivityLog = entityCreation.CreateUserActivityLog(user, ActionTypeEnum.ChangePassword, HttpContext);

        var affectedEntities = new List<string>
        {
            nameof(UserModel),
            nameof(UserActivityLogModel)
        };
        
        var auditLog = entityCreation.CreateAuditLog(user, ActionTypeEnum.ChangePassword, affectedEntities);

        await uows.Users.AddAsync(user);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }
}