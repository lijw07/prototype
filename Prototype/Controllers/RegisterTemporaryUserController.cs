using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(
    IVerificationService verificationService,
    ITemporaryUserFactoryService tempUserFactory,
    IEmailNotificationService emailNotificationService,
    SentinelContext context)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "Email already exists!" });

        var verificationCode = verificationService.GenerateVerificationCode();
        var tempUser = tempUserFactory.CreateFromRequest(request, verificationCode);
        await emailNotificationService.SendVerificationEmail(tempUser.Email, verificationCode);
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration successful. Please check your email to verify your account." });
    }
}