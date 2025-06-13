using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class EntityCreationService(
    IUserFactoryService userFactory,
    IUserActivityLogFactoryService activityLogFactory,
    IAuditLogFactoryService auditLogFactory,
    IUserRecoveryRequestFactoryService recovery)
    : IEntityCreationService
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
    
    public AuditLogModel CreateAuditLog(UserModel user, ActionTypeEnum action, List<string> affectedTables) =>
        auditLogFactory.CreateAuditLog(user, action, affectedTables);
    
    // IUserRecoveryRequestFactoryService
    public UserRecoveryRequestModel CreateUserRecoveryRequest(UserModel user, ForgotUserRequestDto dto, string token) =>
        recovery.CreateUserRecoveryRequest(user, dto, token);
}