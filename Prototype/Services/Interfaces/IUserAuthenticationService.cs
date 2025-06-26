using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;

namespace Prototype.Services.Interfaces;

public interface IUserAuthenticationService
{
    Task<LoginResponseDto> RegisterTemporaryUserAsync(RegisterRequestDto request);
    Task<LoginResponseDto> ForgotPasswordAsync(ForgotUserRequestDto request);
    Task<LoginResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<LoginResponseDto> RegisterNewUser(string token);
    Task<LoginResponseDto> UpdateUserAsync(UpdateUserRequestDto request);
    Task<bool> ValidatePasswordAsync(string userId, string password);
    Task<string> GeneratePasswordResetTokenAsync(string email);
}