using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Services.Interfaces;
using System.Security.Claims;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController(
    IUnitOfWorkService unitOfWork,
    IEntityCreationFactoryService entityFactory,
    IEmailNotificationService emailService,
    IJwtTokenService jwtTokenService,
    SentinelContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (!jwtTokenService.ValidateToken(token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Registered account does not exist!");

        var tempUser = await dbContext.TemporaryUsers
            .FirstOrDefaultAsync(t => t.Email == email);

        if (tempUser is null)
            return BadRequest("Requested account does not exist!");

        // Promote the temporary user to a permanent user
        var newUser = entityFactory.CreateUserFromTemporary(tempUser);
        await unitOfWork.Users.AddAsync(newUser);
        await unitOfWork.SaveChangesAsync();
        
        unitOfWork.TemporaryUser.Delete(tempUser);
        await dbContext.SaveChangesAsync();

        await emailService.SendAccountCreationEmail(newUser.Email, newUser.Username);

        return Ok("Your email has been verified!");
    }
}