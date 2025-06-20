using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.Responses;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class UserAccountService : IUserAccountService
{
    private readonly SentinelContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailNotificationFactoryService _emailService;
    private readonly ValidationService _validationService;
    private readonly TransactionService _transactionService;
    private readonly PasswordEncryptionService _passwordService;
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(
        SentinelContext context,
        IJwtTokenService jwtTokenService,
        IEmailNotificationFactoryService emailService,
        ValidationService validationService,
        TransactionService transactionService,
        PasswordEncryptionService passwordService,
        ILogger<UserAccountService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _validationService = validationService;
        _transactionService = transactionService;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserModel?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserModel?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<LoginResponse> RegisterTemporaryUserAsync(RegisterRequestDto request)
    {
        return await _transactionService.ExecuteInTransactionAsync(async () =>
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

            var token = _jwtTokenService.BuildUserClaims(request, ActionTypeEnum.Register);

            var tempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName ?? "",
                LastName = request.LastName ?? "",
                PhoneNumber = request.PhoneNumber ?? "",
                PasswordHash = _passwordService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                Token = token
            };

            _context.TemporaryUsers.Add(tempUser);
            await _context.SaveChangesAsync();
            
            // Note: Cannot create audit log for temporary user as it requires a real UserId from Users table
            // Audit log will be created when temporary user is converted to permanent user
            
            // Send verification email to temporary user
            try 
            {
                await _emailService.SendVerificationEmailAsync(request.Email, token);
                _logger.LogInformation("Verification email sent successfully to {Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification email to {Email}, but user was created", request.Email);
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
        return await _transactionService.ExecuteInTransactionAsync(async () =>
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

            var token = _jwtTokenService.BuildUserClaims(user, ActionTypeEnum.ForgotPassword);
            
            await _emailService.SendPasswordResetEmailAsync(request.Email, token);

            await CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ForgotPassword, "Password reset requested");

            return new LoginResponse
            {
                Success = true,
                Message = "Password reset email sent"
            };
        });
    }

    public async Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequestDto request)
    {
        return await _transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!_jwtTokenService.ValidateToken(request.Token, out var principal))
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

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Create audit log for password reset
            await CreateAuditLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "User password reset successfully");
            
            // Create user activity log for password reset
            await CreateUserActivityLogAsync(user.UserId, ActionTypeEnum.ResetPassword, "Password reset");

            return new LoginResponse
            {
                Success = true,
                Message = "Password reset successful"
            };
        });
    }

    public async Task<LoginResponse> VerifyUserAsync(string token)
    {
        return await _transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!_jwtTokenService.ValidateToken(token, out var principal))
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

            // Check if this is temporary user verification by looking up the token
            var tempUser = await _context.TemporaryUsers.FirstOrDefaultAsync(u => u.Token == token);
            if (tempUser != null)
            {
                // Note: Cannot create audit log for temporary user as it requires a real UserId from Users table
                // Audit log will be created when temporary user is converted to permanent user

                // Valid temporary user verification - generate a new token for password reset
                var resetToken = _jwtTokenService.BuildUserClaims(new RegisterRequestDto 
                { 
                    Email = tempUser.Email,
                    Username = tempUser.Username,
                    FirstName = tempUser.FirstName,
                    LastName = tempUser.LastName,
                    PhoneNumber = tempUser.PhoneNumber,
                    Password = tempUser.PasswordHash
                }, ActionTypeEnum.ResetPassword);

                return new LoginResponse
                {
                    Success = true,
                    Message = "Email verified successfully. Please set your new password to complete registration.",
                    Token = resetToken,
                    User = new UserDto
                    {
                        UserId = tempUser.TemporaryUserId,
                        FirstName = tempUser.FirstName,
                        LastName = tempUser.LastName,
                        Username = tempUser.Username,
                        Email = tempUser.Email,
                        PhoneNumber = tempUser.PhoneNumber,
                        IsTemporary = true
                    }
                };
            }

            return new LoginResponse
            {
                Success = false,
                Message = "Invalid or expired verification token"
            };
        });
    }

    public async Task<LoginResponse> RegisterNewUser(string token)
    {
        return await _transactionService.ExecuteInTransactionAsync(async () =>
        {
            if (!_jwtTokenService.ValidateToken(token, out var principal))
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

            var tempUser = await _context.TemporaryUsers.FirstOrDefaultAsync(u => u.Email == email);
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
                    CreatedAt = tempUser.CreatedAt,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                _context.TemporaryUsers.Remove(tempUser);
                await _context.SaveChangesAsync();

                // Create audit log for user account creation
                await CreateAuditLogAsync(newUser.UserId, ActionTypeEnum.Register,
                    "Temporary user converted to permanent user account");

                // Create user activity log for password reset/account activation
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

    private async Task CreateUserActivityLogAsync(Guid userId, ActionTypeEnum action, string description)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                User = user,
                IpAddress = "127.0.0.1", // TODO: Would need HttpContext for real IP
                DeviceInformation = "Unknown", // TODO: Would need HttpContext for real device info
                ActionType = action,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }

    private async Task CreateAuditLogAsync(Guid userId, ActionTypeEnum action, string metadata)
    {
        var user = await _context.Users.FindAsync(userId);
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

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}