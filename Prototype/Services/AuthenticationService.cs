using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class AuthenticationService(
    SentinelContext context,
    IJwtTokenService jwtTokenService,
    PasswordEncryptionService passwordService,
    ValidationService validationService,
    ILogger<AuthenticationService> logger)
    : IAuthenticationService
{
    public async Task<LoginResponseDto> AuthenticateAsync(LoginRequestDto request)
    {
        try
        {
            // Validation
            var validationResult = validationService.ValidateLoginRequest(request);
            if (!validationResult.IsSuccess)
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Validation failed"
                };

            // Find user
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Create an activity log
            var userActivityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = user.UserId,
                User = user,
                IpAddress = "127.0.0.1", // Would need HttpContext to get real IP
                DeviceInformation = "Unknown", // Would need HttpContext to get real device info
                ActionType = ActionTypeEnum.Login,
                Description = "User logged in",
                Timestamp = DateTime.UtcNow
            };

            context.UserActivityLogs.Add(userActivityLog);
            await context.SaveChangesAsync();

            // Generate token
            var token = jwtTokenService.BuildUserClaims(user, ActionTypeEnum.Login);
            
            logger.LogInformation("Successful login for user: {Username}", user.Username);
            return new LoginResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during authentication for username: {Username}", request.Username);
            return new LoginResponseDto
            {
                Success = false,
                Message = "An error occurred during authentication"
            };
        }
    }
}