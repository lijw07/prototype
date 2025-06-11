using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserActivityLogFactoryService
{
    UserActivityLogModel CreateUserActivityLog(UserModel user, ActionTypeEnum action, HttpContext context);
}