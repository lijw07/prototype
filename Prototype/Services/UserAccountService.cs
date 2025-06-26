using Microsoft.EntityFrameworkCore;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserAccountService(
    SentinelContext context,
    IJwtTokenService jwtTokenService,
    IEmailNotificationFactoryService emailService,
    ValidationService validationService,
    TransactionService transactionService,
    PasswordEncryptionService passwordService,
    ILogger<UserAccountService> logger,
    IHttpContextAccessor httpContextAccessor,
    ICacheInvalidationService cacheInvalidation)
    : IUserAccountService
{
    private readonly ValidationService _validationService = validationService;

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

    public async Task<LoginResponseDto> RegisterTemporaryUserAsync(RegisterRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var existingUser = await GetUserByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return new LoginResponseDto
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
            }
            
            return new LoginResponseDto
            {
                Success = true,
                Message = "Temporary user account created successfully. A verification email has been sent to complete the registration.",
                Token = token
            };
        });
    }

    public async Task<LoginResponseDto> ForgotPasswordAsync(ForgotUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserByEmailAsync(request.Email);
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
            
            var user = await GetUserByEmailAsync(email);
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
            await context.SaveChangesAsync();

            // Create an audit log for password reset
            await CreateAuditLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "User password reset successfully");
            
            // Create a user activity log for password reset
            await CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "Password reset");

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

                return new LoginResponseDto
                {
                    Success = true,
                    Message = "Password set successfully! Your account is now active and you can login."
                };
            }

            return new LoginResponseDto
            {
                Success = false,
                Message = "User not found or already registered"
            };
        });
    }

    public async Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum action, string description)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var ipAddress = GetClientIpAddress(httpContext);
            var deviceInfo = GetDeviceInformation(httpContext);

            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                User = user,
                IpAddress = ipAddress,
                DeviceInformation = deviceInfo,
                ActionType = action,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            context.UserActivityLogs.Add(activityLog);
            // Note: SaveChanges will be called by the transaction service
        }
    }

    private string GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        // Check for forwarded IP first (for reverse proxy scenarios)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to remote IP address
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetDeviceInformation(HttpContext? httpContext)
    {
        if (httpContext == null)
            return "Unknown";

        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        // Extract basic device information from User-Agent
        var deviceInfo = new List<string>();

        // Check for mobile devices
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Mobile");
        else if (userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Tablet");
        else
            deviceInfo.Add("Desktop");

        // Extract browser information
        if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Chrome");
        else if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Firefox");
        else if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Safari");
        else if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Edge");

        // Extract OS information
        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Windows");
        else if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("macOS");
        else if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Linux");
        else if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("Android");
        else if (userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
            deviceInfo.Add("iOS");

        return deviceInfo.Count > 0 ? string.Join(", ", deviceInfo) : "Unknown";
    }

    public async Task<LoginResponseDto> UpdateUserAsync(UpdateUserRequestDto request)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserByIdAsync(request.UserId);
            if (user == null)
            {
                return new LoginResponseDto
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
                return new LoginResponseDto
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
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Email is already taken"
                };
            }

            // Update user properties
            // Store old values for cache invalidation
            var oldEmail = user.Email;
            var oldUsername = user.Username;

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Username = request.Username;
            user.Email = request.Email;
            user.PhoneNumber = request.PhoneNumber ?? "";
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Invalidate cache for both old and new values
            await cacheInvalidation.InvalidateUserCacheAsync(user.UserId, oldEmail, oldUsername);
            
            if (oldEmail != user.Email || oldUsername != user.Username)
            {
                await cacheInvalidation.InvalidateUserCacheAsync(user.UserId, user.Email, user.Username);
            }

            // Invalidate dashboard stats if user count might change
            await cacheInvalidation.InvalidateDashboardCacheAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "User updated successfully"
            };
        });
    }

    public async Task<LoginResponseDto> DeleteUserAsync(Guid userId)
    {
        return await transactionService.ExecuteInTransactionAsync(async () =>
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "User not found"
                };
            }

            // Store user data for cache invalidation
            var userEmail = user.Email;
            var userUsername = user.Username;

            // Remove user from database
            context.Users.Remove(user);
            await context.SaveChangesAsync();

            // Invalidate all user-related cache
            await cacheInvalidation.InvalidateAllUserSpecificCacheAsync(userId);
            
            // Invalidate global statistics
            await cacheInvalidation.InvalidateDashboardCacheAsync();
            await cacheInvalidation.InvalidateAnalyticsCacheAsync();

            return new LoginResponseDto
            {
                Success = true,
                Message = "User deleted successfully"
            };
        });
    }


    public async Task CreateAuditLogAsync(Guid userId, ActionTypeEnum action, string metadata)
    {
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            var auditLog = new AuditLogModel
            {
                AuditLogId = Guid.NewGuid(),
                UserId = userId,
                User = user,
                ActionType = action,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            context.AuditLogs.Add(auditLog);
        }
    }
}