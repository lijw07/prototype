using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserAccountService
{
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<UserModel?> GetUserByIdAsync(Guid userId);
    Task<LoginResponse> RegisterTemporaryUserAsync(RegisterRequestDto request);
    Task<LoginResponse> ForgotPasswordAsync(ForgotUserRequestDto request);
    Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<LoginResponse> VerifyUserAsync(string token);
    Task<LoginResponse> RegisterNewUser(string token);
}