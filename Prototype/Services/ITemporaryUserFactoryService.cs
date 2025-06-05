using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;

public interface ITemporaryUserFactoryService
{
    TemporaryUserModel CreateTemporaryUserFromRequest(RegisterRequest request, string verificationCode);
    UserModel CreateUserFromTemporaryUser(TemporaryUserModel tempUser);
}