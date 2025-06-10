using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class RegisterTemporaryUserController(
    IEntitySaveService<TemporaryUserModel> tempUserService,
    IVerificationService verificationService,
    IEntityCreationFactoryService tempUserFactory,
    IEmailNotificationService emailNotificationService,
    SentinelContext context)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto requestDto)
    {
        if (await context.Users.AnyAsync(u => u.Email == requestDto.Email))
            return Conflict(new { message = "Email already exists!" });

        var verificationCode = verificationService.GenerateVerificationCode();
        var tempUser = tempUserFactory.CreateTemporaryUserFromRequest(requestDto, verificationCode);
        var createdUser = await tempUserService.CreateAsync(tempUser);
        await emailNotificationService.SendVerificationEmail(createdUser.Email, verificationCode);
        return Ok(new { id = tempUser.TemporaryUserId, message = "Registration successful. Please check your email to verify your account." });
    }
}