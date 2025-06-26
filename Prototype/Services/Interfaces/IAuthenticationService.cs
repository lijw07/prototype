using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;

namespace Prototype.Services.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request);
}