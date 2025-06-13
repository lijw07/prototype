using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class JwtTokenFactoryService(IConfiguration config) : IJwtTokenService
{
    private readonly string _key = config["JwtSettings:Key"]!;
    private readonly string _issuer = config["JwtSettings:Issuer"]!;
    private readonly string _audience = config["JwtSettings:Audience"]!;
    private readonly double _defaultExpiry = double.Parse(config["JwtSettings:ExpiresInMinutes"]!);

    private string GenerateToken(IEnumerable<Claim> claims, double? expiresInMinutes = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes ?? _defaultExpiry),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    //TODO: Add permission claims
    public string BuildUserClaims(UserModel user, ActionTypeEnum action)
    {
        //var permission = user.UserPermissions.Permission.PermissionId.ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("ActionType", action.ToString())
        };

        return GenerateToken(claims, 15);
    }

    public string BuildUserClaims(RegisterRequestDto requestDto, ActionTypeEnum action)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, requestDto.Email),
            new Claim("ActionType", action.ToString())
        };
        
        return GenerateToken(claims, 15);
    }

    public bool ValidateToken(string token, out ClaimsPrincipal principal)
    {
        principal = null!;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            principal = tokenHandler.ValidateToken(token, validationParams, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }
}