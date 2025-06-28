using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserAccountService(
    SentinelContext context,
    IJwtTokenService jwtTokenService,
    IEmailNotificationFactoryService emailService,
    TransactionService transactionService,
    IPasswordEncryptionService passwordService,
    ILogger<UserAccountService> logger,
    IHttpContextAccessor httpContextAccessor,
    IHttpContextParsingService httpContextParsingService,
    IAuditLogService auditLogService)
    : IUserAccountService
{

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        return await context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<LoginResponse> RegisterTemporaryUserAsync(RegisterRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "User already exists"
                };
            }

            var token = jwtTokenService.BuildUserClaims(request, ActionTypeEnum.Register);

            var tempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName ?? "",
                LastName = request.LastName ?? "",
                PhoneNumber = request.PhoneNumber ?? "",
                PasswordHash = passwordService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                Token = token
            };

            context.TemporaryUsers.Add(tempUser);
            await context.SaveChangesAsync();
            
            // Note: Cannot create audit log for temporary user as it requires a real UserId from Users table
            // Audit log will be created when temporary user is converted to permanent user
            
            // Send verification email to temporary user
            try 
            {
                await emailService.SendVerificationEmailAsync(request.Email, token);
                logger.LogInformation("Verification email sent successfully to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send verification email to {Email}, but user was created", request.Email);
                // TODO: Continue even if email fails - user can be verified manually
            }
            
            return new LoginResponse
            {
                Success = true,
                Message = "Temporary user account created successfully. A verification email has been sent to complete the registration.",
                Token = token
            };
        });
    }

    public async Task<LoginResponse> ForgotPasswordAsync(ForgotUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return new LoginResponse
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
            logger.LogInformation("UserRecoveryRequest saved successfully for user {UserId} with ID {RecoveryId}", user.UserId, userRecovery.UserRecoveryRequestId);

            try
            {
                await emailService.SendPasswordResetEmailAsync(request.Email, token);
                logger.LogInformation("Password reset email sent successfully to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}. Recovery request {RecoveryId} was saved but email failed.", request.Email, userRecovery.UserRecoveryRequestId);
            }

            await CreateAuditLogAsync(user.UserId, ActionTypeEnum.ForgotPassword, "Password reset requested");
            await CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ForgotPassword, "Password reset requested");

            // Save all changes made by audit and activity logging
            await context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "Password reset email sent"
            };
        });
    }

    public async Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!jwtTokenService.ValidateToken(request.Token, out var principal))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid or expired token"
                };
            }

            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }
            
            var user = await GetUserByEmailAsync(email);
            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            user.PasswordHash = passwordService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Create an audit log for password reset
            await CreateAuditLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "User password reset successfully");
            
            // Create a user activity log for password reset
            await CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "Password reset");

            return new LoginResponse
            {
                Success = true,
                Message = "Password reset successful"
            };
        });
    }

    public async Task<LoginResponse> RegisterNewUser(string token)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!jwtTokenService.ValidateToken(token, out var principal))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid or expired token"
                };
            }

            var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid token"
                };
            }

            var tempUser = await context.TemporaryUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (tempUser != null)
            {
                // Convert temporary user to permanent user
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
                    CreatedAt = tempUser.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(newUser);
                context.TemporaryUsers.Remove(tempUser);
                await context.SaveChangesAsync();

                // Create an audit log for user account creation
                await CreateAuditLogAsync(newUser.UserId, ActionTypeEnum.Register,
                    "Temporary user converted to permanent user account");

                // Create a user activity log for password reset/account activation
                await CreateUserActivityLogAsync(newUser.UserId, ActionTypeEnum.ResetPassword,
                    "Account activated and password set");

                return new LoginResponse
                {
                    Success = true,
                    Message = "Password set successfully! Your account is now active and you can login."
                };
            }

            return new LoginResponse
            {
                Success = false,
                Message = "User not found or already registered"
            };
        });
    }

    public async Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum action, string description)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var ipAddress = httpContextParsingService.GetClientIpAddress(httpContext);
        var deviceInfo = httpContextParsingService.GetDeviceInformation(httpContext);

        await auditLogService.CreateUserActivityLogAsync(userId, action, description, ipAddress, deviceInfo);
    }


    public async Task<LoginResponse> UpdateUserAsync(UpdateUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Check if username is already taken by another user
            var existingUserWithUsername = await context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.UserId != request.UserId);
            if (existingUserWithUsername != null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Username is already taken"
                };
            }

            // Check if email is already taken by another user
            var existingUserWithEmail = await context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.UserId != request.UserId);
            if (existingUserWithEmail != null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Email is already taken"
                };
            }

            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Username = request.Username;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber ?? "";
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "User updated successfully"
            };
        });
    }

    public async Task<LoginResponse> DeleteUserAsync(Guid userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "User not found"
            };
        }

        // Delete related records first to avoid foreign key constraint violations
        
        // Delete UserApplications
        var userApplications = await context.UserApplications
            .Where(ua => ua.UserId == userId)
            .ToListAsync();
        if (userApplications.Any())
        {
            context.UserApplications.RemoveRange(userApplications);
        }

        // Delete UserActivityLogs
        var userActivityLogs = await context.UserActivityLogs
            .Where(ual => ual.UserId == userId)
            .ToListAsync();
        if (userActivityLogs.Any())
        {
            context.UserActivityLogs.RemoveRange(userActivityLogs);
        }

        // Delete AuditLogs
        var auditLogs = await context.AuditLogs
            .Where(al => al.UserId == userId)
            .ToListAsync();
        if (auditLogs.Any())
        {
            context.AuditLogs.RemoveRange(auditLogs);
        }

        // Delete UserRecoveryRequests
        var recoveryRequests = await context.UserRecoveryRequests
            .Where(urr => urr.UserId == userId)
            .ToListAsync();
        if (recoveryRequests.Any())
        {
            context.UserRecoveryRequests.RemoveRange(recoveryRequests);
        }

        // Delete BulkUploadHistories
        var bulkUploadHistories = await context.BulkUploadHistories
            .Where(buh => buh.UserId == userId)
            .ToListAsync();
        if (bulkUploadHistories.Any())
        {
            context.BulkUploadHistories.RemoveRange(bulkUploadHistories);
        }

        // Finally remove the user
        context.Users.Remove(user);

        return new LoginResponse
        {
            Success = true,
            Message = "User deleted successfully"
        };
    }


    public async Task CreateAuditLogAsync(Guid userId, ActionTypeEnum action, string metadata)
    {
        await auditLogService.CreateAuditLogAsync(userId, action, metadata);
    }
}