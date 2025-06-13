using Microsoft.AspNetCore.Mvc;
using Prototype.Services.Interfaces;
using System.Security.Claims;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController(
    IUnitOfWorkService unitOfWork,
    IEntityCreationService entity,
    IEmailNotificationService emailService,
    IJwtTokenService jwtTokenService,
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (!jwtTokenService.ValidateToken(token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");
        
        var tempUser = await userAccessor.FindTemporaryUserByEmail(
            principal.FindFirst(ClaimTypes.Email)?.Value);

        if (tempUser is null || string.IsNullOrWhiteSpace(tempUser.Email))
            return BadRequest("Registered account does not exist!");
        
        var newUser = entity.CreateUserFromTemporary(tempUser);
        await unitOfWork.Users.AddAsync(newUser);
    
        unitOfWork.TemporaryUser.Delete(tempUser);
        await unitOfWork.SaveChangesAsync();

        await emailService.SendAccountCreationEmail(newUser.Email, newUser.Username);
        return Ok("Your email has been verified!");
    }
}