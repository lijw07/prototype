using Prototype.Models;

namespace Prototype.Services.Interfaces;

public interface IUserActivityLogFactoryService
{
    UserActivityLogModel CreateFromLogin(UserModel user, HttpContext context);
    UserActivityLogModel CreateFromPasswordChange(UserModel user, HttpContext context);
    
    UserActivityLogModel CreateFromDataDump(UserModel user, HttpContext context);
}