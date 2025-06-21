using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly SentinelContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordEncryptionService _passwordService;
    private readonly ValidationService _validationService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        SentinelContext context,
        IJwtTokenService jwtTokenService,
        PasswordEncryptionService passwordService,
        ValidationService validationService,
        ILogger<AuthenticationService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordService = passwordService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<LoginResponse> AuthenticateAsync(LoginRequestDto request)
    {
        try
        {
            // Validation
            var validationResult = _validationService.ValidateLoginRequest(request);
            if (!validationResult.IsSuccess)
                return new LoginResponse
                {
                    Success = false,
                    Message = "Validation failed"
                };

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Create activity log
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

            _context.UserActivityLogs.Add(userActivityLog);
            await _context.SaveChangesAsync();

            // Generate token
            var token = _jwtTokenService.BuildUserClaims(user, ActionTypeEnum.Login);
            
            _logger.LogInformation("Successful login for user: {Username}", user.Username);
            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for username: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = "An error occurred during authentication"
            };
        }
    }
}