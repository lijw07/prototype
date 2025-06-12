using System.Security.Claims;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IJwtTokenService
{
    string BuildUserClaims(UserModel user, ActionTypeEnum action);
    string BuildUserClaims(RegisterRequestDto requestDto, ActionTypeEnum action);
    bool ValidateToken(string token, out ClaimsPrincipal principal);

}