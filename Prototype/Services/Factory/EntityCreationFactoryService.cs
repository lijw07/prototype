using Microsoft.AspNetCore.Http;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class EntityCreationFactoryService(
    IUserFactoryService userFactory,
    IUserActivityLogFactoryService activityLogFactory,
    IAuditLogFactoryService auditLogFactory,
    IUserRecoveryRequestFactoryService recoveryFactory)
    : IEntityCreationFactoryService
{
    // IUserFactoryService
    public TemporaryUserModel CreateTemporaryUser(RegisterRequestDto dto, string token) =>
        userFactory.CreateTemporaryUser(dto, token);

    public UserModel CreateUserFromTemporary(TemporaryUserModel tempUser) =>
        userFactory.CreateUserFromTemporary(tempUser);

    // IUserActivityLogFactoryService
    public UserActivityLogModel CreateUserActivityLog(UserModel user, ActionTypeEnum action, HttpContext context) =>
        activityLogFactory.CreateUserActivityLog(user, action, context);

    // IAuditLogFactoryService
    public AuditLogModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto request, UserRecoveryRequestModel recoveryLog) =>
        auditLogFactory.CreateFromForgotUser(user, request, recoveryLog);

    public AuditLogModel CreateFromResetPassword(UserRecoveryRequestModel recoveryRequest) =>
        auditLogFactory.CreateFromResetPassword(recoveryRequest);

    public AuditLogModel CreateFromPasswordChange(UserModel user) =>
        auditLogFactory.CreateFromPasswordChange(user);

    public AuditLogModel CreateFromDataDump(UserModel user, DataDumpRequestDto request, List<string> affectedTables) =>
        auditLogFactory.CreateFromDataDump(user, request, affectedTables);

    // IUserRecoveryRequestFactoryService
    public UserRecoveryRequestModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto dto, string token) =>
        recoveryFactory.CreateFromForgotUser(user, dto, token);
}