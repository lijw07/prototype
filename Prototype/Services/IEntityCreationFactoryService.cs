using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;

/// <summary>
/// IEntityCreationFactoryService Is responsible for creating Entities.
/// </summary>
public interface IEntityCreationFactoryService
{
    TemporaryUserModel CreateTemporaryUserFromRequest(RegisterRequest request, string verificationCode);
    UserModel CreateUserFromTemporaryUser(TemporaryUserModel tempUser);
    UserActivityLogModel CreateUserActivityLogFromLogin(UserModel user,  HttpContext httpContext);
    UserRecoveryRequestModel CreateUserRecoveryRequestFronForgotUser(UserModel user, ForgotUserRequest request, string generateVerificationCode);
    AuditLogModel CreateAuditLogFromForgotUser(UserModel user, ForgotUserRequest request, UserRecoveryRequestModel userRecoveryLog);
}