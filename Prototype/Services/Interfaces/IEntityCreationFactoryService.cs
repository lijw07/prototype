using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;

/// <summary>
/// IEntityCreationFactoryService Is responsible for creating Entities.
/// </summary>
public interface IEntityCreationFactoryService
{
    TemporaryUserModel CreateTemporaryUserFromRequest(RegisterRequestDto requestDto, string verificationCode);
    UserModel CreateUserFromTemporaryUser(TemporaryUserModel tempUser);
    UserActivityLogModel CreateUserActivityLogFromLogin(UserModel user,  HttpContext httpContext);
    UserRecoveryRequestModel CreateUserRecoveryRequestFronForgotUser(UserModel user, ForgotUserRequestDto requestDto, string generateVerificationCode);
    AuditLogModel CreateAuditLogFromForgotUser(UserModel user, ForgotUserRequestDto requestDto, UserRecoveryRequestModel userRecoveryLog);
    AuditLogModel CreateAuditLogFromResetPassword(UserRecoveryRequestModel userRecoveryRequest);
}