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
}