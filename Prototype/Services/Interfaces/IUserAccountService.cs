using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserAccountService
{
    Task<UserModel?> GetUserByEmailAsync(string email);
    Task<UserModel?> GetUserByUsernameAsync(string username);
    Task<UserModel?> GetUserByIdAsync(Guid userId);
    Task<List<UserModel>> GetAllUsersAsync();
    Task<LoginResponseDto> RegisterTemporaryUserAsync(RegisterRequestDto request);
    Task<LoginResponseDto> ForgotPasswordAsync(ForgotUserRequestDto request);
    Task<LoginResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    Task<LoginResponseDto> RegisterNewUser(string token);
    Task<LoginResponseDto> UpdateUserAsync(UpdateUserRequestDto request);
    Task<LoginResponseDto> DeleteUserAsync(Guid userId);
    Task CreateAuditLogAsync(Guid currentUserUserId, ActionTypeEnum update, string metadata);
    Task CreateUserActivityLogAsync(Guid currentUserUserId, ActionTypeEnum update, string userUpdateRequest);
}