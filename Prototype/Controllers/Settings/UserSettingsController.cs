using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using static BCrypt.Net.BCrypt;

namespace Prototype.Controllers.Settings;

[Authorize]
[ApiController]
[Route("settings/user")]
public class UserSettingsController(
    IEntityCreationFactoryService entityCreationFactory,
    IAuthenticatedUserAccessor userAccessor,
    IUnitOfWorkFactoryService uows) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        
        if (user is null)
            return NotFound("User not found.");
        
        return Ok(new UserDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,

        });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var user = await userAccessor.GetCurrentUserAsync(User);
        
        if (user is null)
            return NotFound("User not found.");

        if (!request.NewPassword.Equals(request.ReTypeNewPassword))
            return BadRequest(new { message = "New password does not match." });
        
        if (!Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.Now;

        var userActivityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.ChangePassword, HttpContext);

        var affectedEntities = new List<string>
        {
            nameof(UserModel),
            nameof(UserActivityLogModel)
        };
        
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.ChangePassword, affectedEntities);

        uows.Users.Update(user);
        await uows.UserActivityLogs.AddAsync(userActivityLog);
        await uows.AuditLogs.AddAsync(auditLog);
        await uows.SaveChangesAsync();

        return Ok(new { message = "Password updated successfully." });
    }
}