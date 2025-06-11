using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class AuditLogFactoryService : IAuditLogFactoryService
{
    public AuditLogModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto requestDto,
        UserRecoveryRequestModel userRecoveryLog)
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
            userRecoveryLog.RequestedAt
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

    public AuditLogModel CreateFromResetPassword(UserRecoveryRequestModel userRecoveryRequest)
    {
        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = userRecoveryRequest.UserId,
            User = userRecoveryRequest.User,
            ActionType = ActionTypeEnum.ChangePassword,
            Description = $"User {userRecoveryRequest.User.Username} successfully reset their password using token {userRecoveryRequest.VerificationCode}.",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                userRecoveryRequest.UserRecoveryRequestId,
                userRecoveryRequest.UserId,
                userRecoveryRequest.User.Email,
                userRecoveryRequest.VerificationCode,
                AffectedTable = nameof(UserModel),
                RecoveryType = userRecoveryRequest.UserRecoveryType.ToString(),
                userRecoveryRequest.RequestedAt,
                ResetAt = DateTime.Now
            }),
            CreatedAt = DateTime.Now
        };
    }

    public AuditLogModel CreateFromPasswordChange(UserModel user)
    {
        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = ActionTypeEnum.ChangePassword,
            Description = $"User {user.Username} successfully reset their password!",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                user.UserId,
                user.Email,
                user,
                AffectedTable = nameof(UserModel),
                RecoveryType = nameof(ActionTypeEnum.ChangePassword),
                RequestedAt = user.CreatedAt,
                ResetAt = DateTime.Now
            }),
            CreatedAt = DateTime.Now
        };
    }

    public AuditLogModel CreateFromDataDump(UserModel user, DataDumpRequestDto requestDto, List<string> affectedTables)
    {
        return new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ActionType = ActionTypeEnum.DataDumpUpload,
            Description = $"User {user.Username} uploaded a data dump affecting: {string.Join(", ", affectedTables)}",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                user.UserId,
                user.Email,
                DataDumpType = requestDto.DataDumpParseType.ToString(),
                AffectedTables = affectedTables,
                UploadedFiles = requestDto.File.Select(f => f.FileName).ToList(),
                UploadedAt = DateTime.UtcNow
            }),
            CreatedAt = DateTime.UtcNow
        };
    }
}