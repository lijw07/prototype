using Microsoft.EntityFrameworkCore;
using Prototype.Constants;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Exceptions;
using Prototype.Models;
using Prototype.Services.Interfaces;
using Prototype.Services.Validators;

namespace Prototype.Services;

public class AuthenticationService(
    SentinelContext context,
    IJwtTokenService jwtTokenService,
    IPasswordEncryptionService passwordService,
    LoginRequestValidator loginValidator,
    ILogger<AuthenticationService> logger,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogService auditLogService,
    IHttpContextParsingService httpContextParsingService)
    : IAuthenticationService
{
    public async Task<LoginResponse> AuthenticateAsync(LoginRequestDto request)
    {
        try
        {
            // Validation
            var validationResult = await loginValidator.ValidateAsync(request);
            if (!validationResult.IsSuccess)
                return new LoginResponse
                {
                    Success = false,
                    Message = ApplicationConstants.ErrorMessages.InvalidRequest,
                    Errors = validationResult.Errors
                };

            // Find user
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return new LoginResponse
                {
                    Success = false,
                    Message = ApplicationConstants.ErrorMessages.InvalidCredentials
                };
            }

            // Update last login timestamp
            user.LastLogin = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Create activity log using centralized service
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
            var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);
            
            await auditLogService.CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.Login, "User logged in", ipAddress, deviceInfo);
            await context.SaveChangesAsync();

            // Generate token
            var token = jwtTokenService.BuildUserClaims(user, ActionTypeEnum.Login);
            
            logger.LogInformation("Successful login for user: {Username}", user.Username);
            return new LoginResponse
            {
                Success = true,
                Message = ApplicationConstants.SuccessMessages.LoginSuccess,
                Token = token
            };
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed during authentication for username: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning(ex, "Authentication failed for username: {Username} from IP: {IpAddress}", 
                request.Username, ex.IpAddress);
            return new LoginResponse
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (DataNotFoundException ex)
        {
            logger.LogWarning(ex, "User not found during authentication: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = ApplicationConstants.ErrorMessages.InvalidCredentials
            };
        }
        catch (ExternalServiceException ex)
        {
            logger.LogError(ex, "External service failure during authentication for username: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = "Authentication service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during authentication for username: {Username}", request.Username);
            return new LoginResponse
            {
                Success = false,
                Message = ApplicationConstants.ErrorMessages.ServerError
            };
        }
    }
}