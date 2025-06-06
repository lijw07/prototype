using Microsoft.AspNetCore.Mvc;

namespace Prototype.DTOs;

public class ResetPasswordRequestDto
{
    [FromQuery]
    public required string Token { get; set; }
    public required string password { get; set; }
}