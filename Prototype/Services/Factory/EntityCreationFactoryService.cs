using Microsoft.AspNetCore.Http;
using Prototype.DTOs;
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
    public TemporaryUserModel CreateTemporaryUser(RegisterRequestDto dto, string verificationCode) =>
        userFactory.CreateTemporaryUser(dto, verificationCode);

    public UserModel CreateUserFromTemporary(TemporaryUserModel tempUser) =>
        userFactory.CreateUserFromTemporary(tempUser);

    // IUserActivityLogFactoryService
    public UserActivityLogModel CreateFromLogin(UserModel user, HttpContext context) =>
        activityLogFactory.CreateFromLogin(user, context);

    public UserActivityLogModel CreateFromPasswordChange(UserModel user, HttpContext context) =>
        activityLogFactory.CreateFromPasswordChange(user, context);

    public UserActivityLogModel CreateFromDataDump(UserModel user, HttpContext context) =>
        activityLogFactory.CreateFromDataDump(user, context);

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
    public UserRecoveryRequestModel CreateFromForgotUser(UserModel user, ForgotUserRequestDto dto, string verificationCode) =>
        recoveryFactory.CreateFromForgotUser(user, dto, verificationCode);
}