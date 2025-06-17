using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserApplicationFactoryService
{
    UserApplicationModel CreateUserApplication(UserModel user, ApplicationModel application);
}