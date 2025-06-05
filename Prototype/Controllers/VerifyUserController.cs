using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Controllers;

[ApiController]
[Route("verify")]
public class VerifyUserController(
    IEntityCreationService<UserModel> tempUserService,
    ITemporaryUserFactoryService tempUserFactory,
    IEmailNotificationService emailNotificationService,
    SentinelContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string code)
    {
        var requestedUser = await context.TemporaryUsers
            .FirstOrDefaultAsync(t => t.Email == email && t.VerificationCode == code);

        if (requestedUser == null || requestedUser.CreatedAt.AddHours(24) < DateTime.Now)
            return BadRequest("Invalid or expired verification link.");
        
        var tempUser = tempUserFactory.CreateUserFromTemporaryUser(requestedUser);
        var createUser = await tempUserService.CreateAsync(tempUser);
        context.TemporaryUsers.Remove(requestedUser);
        await context.SaveChangesAsync();
        await emailNotificationService.SendAccountCreationEmail(createUser.Email, createUser.Username);
        return Ok("Your email has been verified. You may now log in.");
    }
}