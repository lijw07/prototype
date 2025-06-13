using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

/// <summary>
/// IEntityCreationFactoryService Is responsible for creating Entities.
/// </summary>
public interface IEntityCreationService :
    IUserFactoryService,
    IUserActivityLogFactoryService,
    IAuditLogFactoryService,
    IUserRecoveryRequestFactoryService
{ }