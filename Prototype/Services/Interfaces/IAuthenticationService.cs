using Prototype.DTOs;
using Prototype.DTOs.Responses;

namespace Prototype.Services.Interfaces;

public interface IAuthenticationService
{
    Task<LoginResponse> AuthenticateAsync(LoginRequestDto request);
}