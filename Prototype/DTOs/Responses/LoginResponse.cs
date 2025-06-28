namespace Prototype.DTOs.Responses;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new List<string>();
}