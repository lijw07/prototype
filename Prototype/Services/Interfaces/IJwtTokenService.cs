using System.Security.Claims;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IJwtTokenService
{
    string BuildUserClaims(UserModel user, JwtPurposeTypeEnum purpose);
    string BuildUserClaims(RegisterRequestDto requestDto, JwtPurposeTypeEnum verification);
    bool ValidateToken(string token, out ClaimsPrincipal principal);

}