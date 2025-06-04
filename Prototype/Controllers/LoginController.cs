using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(SentinelContext context) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new {message = "Invalid username or password"});
        }
        
        return Ok(new {id = user.UserId, message = "Login Successful"});
    }
}