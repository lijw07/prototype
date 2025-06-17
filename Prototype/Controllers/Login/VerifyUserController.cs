using Microsoft.AspNetCore.Mvc;
using Prototype.Services.Interfaces;
using System.Security.Claims;
using Prototype.Utility;

namespace Prototype.Controllers.Login;

[ApiController]
[Route("[controller]")]
public class VerifyUserController(
    IUnitOfWorkFactoryService unitOfWorkFactory,
    IEntityCreationFactoryService entityFactory,
    IEmailNotificationFactoryService emailFactoryService,
    IJwtTokenFactoryService jwtTokenFactoryService,
    IAuthenticatedUserAccessor userAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (!jwtTokenFactoryService.ValidateToken(token, out ClaimsPrincipal principal))
            return BadRequest("Invalid or expired token.");
        
        var tempUser = await userAccessor.FindTemporaryUserByEmail(
            principal.FindFirst(ClaimTypes.Email)?.Value);

        if (tempUser is null || string.IsNullOrWhiteSpace(tempUser.Email))
            return BadRequest("Registered account does not exist!");
        
        var newUser = entityFactory.CreateUserFromTemporary(tempUser);
        await unitOfWorkFactory.Users.AddAsync(newUser);
    
        unitOfWorkFactory.TemporaryUser.Delete(tempUser);
        await unitOfWorkFactory.SaveChangesAsync();

        await emailFactoryService.SendAccountCreationEmail(newUser.Email, newUser.Username);
        return Ok("Your email has been verified!");
    }
}