using Microsoft.AspNetCore.Mvc;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult Login(int id)
    { 
        return Ok(new {id, message = "Login Successful"});
    }
}