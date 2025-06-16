using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UserApplicationFactoryService : IUserApplicationFactoryService
{
    public UserApplicationModel CreateUserApplication(UserModel user, ApplicationModel application)
    {
        return new UserApplicationModel
        {
            UserApplicationId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            ApplicationId = application.ApplicationId,
            Application = application
        };
    }
}