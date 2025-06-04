namespace Prototype.DTOs;

public class EmailVerificationRequest
{
    public required string Email { get; set; }
    public required string VerificationCode { get; set; }
}