using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;
using UAParser;

namespace Prototype.Services;

public class EntityCreationFactoryService : IEntityCreationFactoryService
{
    public TemporaryUserModel CreateTemporaryUserFromRequest(RegisterRequestDto requestDto, string verificationCode)
    {
        return new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = requestDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password),
            FirstName = requestDto.FirstName,
            LastName = requestDto.LastName,
            Email = requestDto.Email,
            PhoneNumber = requestDto.PhoneNumber,
            CreatedAt = DateTime.Now,
            VerificationCode = verificationCode
        };
    }
    
    public UserModel CreateUserFromTemporaryUser(TemporaryUserModel tempUser)
    {
        return new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = tempUser.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempUser.PasswordHash),
            FirstName = tempUser.FirstName,
            LastName = tempUser.LastName,
            Email = tempUser.Email,
            PhoneNumber = tempUser.PhoneNumber,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
    
    public UserActivityLogModel CreateUserActivityLogFromLogin(UserModel user, HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var uaParser = Parser.GetDefault();
        var clientInfo = uaParser.Parse(userAgent);
        var os = clientInfo.OS.ToString();
        var browser = clientInfo.UA.ToString();

        return new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            IPAddress = ipAddress,
            DeviceInformation = $"{browser} on {os}",
            ActionType = ActionTypeEnum.Login,
            Description = $"User {user.Username} logged in from IP {ipAddress} using {browser} on {os}.",
            Timestamp = DateTime.Now
        };
    }

    public UserRecoveryRequestModel CreateUserRecoveryRequestFronForgotUser(UserModel user, ForgotUserRequestDto requestDto, string token)
    {
        return new UserRecoveryRequestModel
        {
            UserRecoveryRequestId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            Token = token,
            UserRecoveryType = requestDto.UserRecoveryType,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddMinutes(30)
        };
    }

    public AuditLogModel CreateAuditLogFromForgotUser(UserModel user, ForgotUserRequestDto requestDto, UserRecoveryRequestModel userRecoveryLog)
    {
        var action = requestDto.UserRecoveryType == UserRecoveryTypeEnum.PASSWORD
            ? ActionTypeEnum.ChangePassword
            : ActionTypeEnum.ForgotUsername;

        var description = action switch
        {
            ActionTypeEnum.ChangePassword => $"User {user.Username} initiated a password reset request.",
            ActionTypeEnum.ForgotUsername => $"User with email {user.Email} requested their username.",
            _ => "User recovery action performed."
        };

        var metadata = new
        {
            user.UserId,
            user.Email,
            RecoveryType = requestDto.UserRecoveryType.ToString(),
            RecoveryRequestId = userRecoveryLog.UserRecoveryRequestId,
            RequestedAt = userRecoveryLog.CreatedAt
        };

        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = action,
            Description = description,
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.Now
        };
    }

    public AuditLogModel CreateAuditLogFromResetPassword(UserRecoveryRequestModel userRecoveryRequest)
    {
        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = userRecoveryRequest.UserId,
            User = userRecoveryRequest.User,
            ActionType = ActionTypeEnum.ChangePassword,
            Description = $"User {userRecoveryRequest.User.Username} successfully reset their password using token {userRecoveryRequest.Token}.",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                userRecoveryRequest.UserRecoveryRequestId,
                userRecoveryRequest.UserId,
                userRecoveryRequest.User.Email,
                userRecoveryRequest.Token,
                AffectedTable = nameof(UserModel),
                RecoveryType = userRecoveryRequest.UserRecoveryType.ToString(),
                RequestedAt = userRecoveryRequest.CreatedAt,
                ResetAt = DateTime.Now
            }),
            CreatedAt = DateTime.Now
        };
    }

    public AuditLogModel CreateAuditLogFromDataDumpService(DataDumpRequestDto requestDto)
    {
        throw new NotImplementedException();
    }
}