using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    private readonly string _key = config["JwtSettings:Key"] ?? throw new InvalidOperationException("JwtSettings:Key is missing");
    private readonly string _issuer = config["JwtSettings:Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is missing");
    private readonly string _audience = config["JwtSettings:Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is missing");
    private readonly double _defaultExpiry = double.Parse(config["JwtSettings:ExpiresInMinutes"] ?? "60");

    public string GenerateToken(IEnumerable<Claim> claims, double? expiresInMinutes = null)
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

    public string BuildUserClaims(UserModel user, ActionTypeEnum action)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("ActionType", action.ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        return GenerateToken(claims, _defaultExpiry);
    }

    public string BuildUserClaims(RegisterRequestDto requestDto, ActionTypeEnum action)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, requestDto.Email),
            new Claim(ClaimTypes.Name, requestDto.Username),
            new Claim("ActionType", action.ToString()),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        
        return GenerateToken(claims, 15);
    }

    public bool ValidateToken(string token, out ClaimsPrincipal principal)
    {
        principal = null!;
        
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireExpirationTime = true
        };

        try
        {
            principal = tokenHandler.ValidateToken(token, validationParams, out _);
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}