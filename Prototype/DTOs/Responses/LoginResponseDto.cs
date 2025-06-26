using Prototype.DTOs.Request;

namespace Prototype.DTOs.Responses;

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserRequestDto? User { get; set; }
}