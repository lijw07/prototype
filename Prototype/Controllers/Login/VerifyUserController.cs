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

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email claim is missing.");

        if (await userAccessor.FindUserByEmail(email) != null)
            return Ok("Account already verified.");
        
        var tempUser = await userAccessor.FindTemporaryUserByEmail(email);
        
        if (tempUser is null)
            return BadRequest("Registered temporary account does not exist.");
        

        var newUser = entityFactory.CreateUserFromTemporary(tempUser);
        await unitOfWorkFactory.Users.AddAsync(newUser);
        unitOfWorkFactory.TemporaryUser.Delete(tempUser);
        await unitOfWorkFactory.SaveChangesAsync();

        await emailFactoryService.SendAccountCreationEmail(newUser.Email, newUser.Username);
        return Ok("Your email has been verified!");
    }
}