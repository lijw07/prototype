
namespace Prototype.Services.Interfaces;

/// <summary>
/// IEntityCreationFactoryService Is responsible for creating Entities.
/// </summary>
public interface IEntityCreationFactoryService :
    IUserFactoryService,
    IUserActivityLogFactoryService,
    IAuditLogFactoryService,
    IUserRecoveryRequestFactoryService,
    IApplicationFactoryService,
    IApplicationLogFactoryService,
    IUserApplicationFactoryService
{ }