using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class EntityCreationFactoryService(
    IUserFactoryService userFactory,
    IUserActivityLogFactoryService activityLogFactory,
    IAuditLogFactoryService auditLogFactory,
    IUserRecoveryRequestFactoryService recoveryFactory,
    IApplicationFactoryService applicationFactory,
    IApplicationLogFactoryService applicationLogFactory,
    IUserApplicationFactoryService userApplicationFactory)
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
    
    public AuditLogModel CreateAuditLog(UserModel user, ActionTypeEnum action, List<string> affectedTables) =>
        auditLogFactory.CreateAuditLog(user, action, affectedTables);
    
    // IUserRecoveryRequestFactoryService
    public UserRecoveryRequestModel CreateUserRecoveryRequest(UserModel user, ForgotUserRequestDto dto, string token) =>
        recoveryFactory.CreateUserRecoveryRequest(user, dto, token);

    // IApplicationFactoryService
    public ApplicationModel CreateApplication(ApplicationRequestDto requestDto) =>
        applicationFactory.CreateApplication(requestDto);
    
    // IApplicationLogFactoryService
    public ApplicationLogModel CreateApplicationLog(ApplicationModel application, ApplicationActionTypeEnum actionType, List<string> affectedEntities) =>
        applicationLogFactory.CreateApplicationLog(application, actionType, affectedEntities);
    
    // IUserApplicationFactoryService
    public UserApplicationModel CreateUserApplication(UserModel user, ApplicationModel application) =>
        userApplicationFactory.CreateUserApplication(user, application);
}