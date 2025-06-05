using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;

public interface ITemporaryUserFactoryService
{
    TemporaryUserModel CreateFromRequest(RegisterRequest request, string verificationCode);
}