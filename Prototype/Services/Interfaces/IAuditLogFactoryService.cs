using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IAuditLogFactoryService
{
    AuditLogModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto requestDto, UserRecoveryRequestModel recoveryLog);
    AuditLogModel CreateFromResetPassword(UserRecoveryRequestModel recoveryRequest);
    AuditLogModel CreateFromPasswordChange(UserModel user);
    AuditLogModel CreateFromDataDump(UserModel user, DataDumpRequestDto requestDto, List<string> affectedTables);
}