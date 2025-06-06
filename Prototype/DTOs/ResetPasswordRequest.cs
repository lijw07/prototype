using Microsoft.AspNetCore.Mvc;

namespace Prototype.DTOs;

public class ResetPasswordRequest
{
    [FromQuery]
    public required string Token { get; set; }
    public string password { get; set; }
}