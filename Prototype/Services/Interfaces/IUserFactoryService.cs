using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserFactoryService
{
    TemporaryUserModel CreateTemporaryUser(RegisterRequestDto dto, string verificationCode);
    UserModel CreateUserFromTemporary(TemporaryUserModel tempUser);
}