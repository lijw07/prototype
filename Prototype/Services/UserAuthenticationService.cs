using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserAuthenticationService(
    SentinelContext context,
    IUserRepository userRepository,
    IUserActivityService activityService,
    IJwtTokenService jwtTokenService,
    IEmailNotificationFactoryService emailService,
    PasswordEncryptionService passwordService,
    TransactionService transactionService,
    ValidationService validationService,
    ICacheInvalidationService cacheInvalidation,
    ILogger<UserAuthenticationService> logger) : IUserAuthenticationService
{
    public async Task<LoginResponseDto> RegisterTemporaryUserAsync(RegisterRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            // Validate the input
            var validationResult = validationService.ValidateRegisterRequest(request);
            if (!validationResult.Success)
            {
                return new LoginResponseDto { Success = false, Message = validationResult.Message };
            }

            // Check if user already exists
            if (await userRepository.UserExistsAsync(request.Email))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "User with this email already exists"
                };
            }

            var tempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = passwordService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            context.TemporaryUsers.Add(tempUser);
            await context.SaveChangesAsync();

            var token = jwtTokenService.BuildUserClaims(tempUser, ActionTypeEnum.Register);

            try
            {
                await emailService.SendVerificationEmailAsync(request.Email, token);
                logger.LogInformation("Verification email sent successfully to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send verification email to {Email}", request.Email);
                // Continue - user can still verify manually if needed
            }

            return new LoginResponseDto
            {
                Success = true,
                Message = "Registration successful. Please check your email for verification."
            };
        });
    }

    public async Task<LoginResponseDto> ForgotPasswordAsync(ForgotUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            var token = jwtTokenService.BuildUserClaims(user, ActionTypeEnum.ForgotPassword);
            
            var userRecovery = new UserRecoveryRequestModel
            {
                UserRecoveryRequestId = Guid.NewGuid(),
                UserId = user.UserId,
                User = user,
                Token = token,
                IsUsed = false,
                RecoveryType = request.UserRecoveryType,
                RequestedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            context.UserRecoveryRequests.Add(userRecovery);
            await context.SaveChangesAsync();

            try
            {
                await emailService.SendPasswordResetEmailAsync(request.Email, token);
                logger.LogInformation("Password reset email sent successfully to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
            }

            await activityService.CreateAuditLogAsync(user.UserId, ActionTypeEnum.ForgotPassword, "Password reset requested");
            await activityService.CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ForgotPassword, "Password reset requested");

            return new LoginResponseDto
            {
                Success = true,
                Message = "Password reset email sent"
            };
        });
    }

    public async Task<LoginResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!jwtTokenService.ValidateToken(request.Token, out var principal))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired token"
                };
            }

            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }
            
            var user = await userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            user.PasswordHash = passwordService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await userRepository.UpdateUserAsync(user);

            await activityService.CreateAuditLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "User password reset successfully");
            await activityService.CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "Password reset");

            return new LoginResponseDto
            {
                Success = true,
                Message = "Password reset successful"
            };
        });
    }

    public async Task<LoginResponseDto> RegisterNewUser(string token)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!jwtTokenService.ValidateToken(token, out var principal))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Invalid or expired token"
                };
            }

            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var tempUser = await context.TemporaryUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (tempUser == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Temporary user not found"
                };
            }

            // Create permanent user from temporary user
            var newUser = new UserModel
            {
                UserId = Guid.NewGuid(),
                FirstName = tempUser.FirstName,
                LastName = tempUser.LastName,
                Username = tempUser.Username,
                Email = tempUser.Email,
                PhoneNumber = tempUser.PhoneNumber,
                PasswordHash = tempUser.PasswordHash,
                IsActive = true,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await userRepository.CreateUserAsync(newUser);

            // Remove temporary user
            context.TemporaryUsers.Remove(tempUser);
            await context.SaveChangesAsync();

            await activityService.CreateAuditLogAsync(newUser.UserId, ActionTypeEnum.Register, "User account verified and activated");
            await activityService.CreateUserActivityLogAsync(newUser.UserId, ActionTypeEnum.Register, "Account verification completed");

            return new LoginResponseDto
            {
                Success = true,
                Message = "User registration completed successfully"
            };
        });
    }

    public async Task<LoginResponseDto> UpdateUserAsync(UpdateUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await userRepository.GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Update user properties
            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await userRepository.UpdateUserAsync(user);
            await cacheInvalidation.InvalidateUserRelatedCacheAsync(request.UserId);

            await activityService.CreateAuditLogAsync(user.UserId, ActionTypeEnum.UpdateUser, "User profile updated");
            await activityService.CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.UpdateUser, "Profile updated");

            return new LoginResponseDto
            {
                Success = true,
                Message = "User updated successfully"
            };
        });
    }

    public async Task<bool> ValidatePasswordAsync(string userId, string password)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return false;

        var user = await userRepository.GetUserByIdAsync(userGuid);
        if (user == null)
            return false;

        return passwordService.VerifyPassword(password, user.PasswordHash);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userRepository.GetUserByEmailAsync(email);
        if (user == null)
            throw new ArgumentException("User not found");

        return jwtTokenService.BuildUserClaims(user, ActionTypeEnum.ForgotPassword);
    }
}