using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;

namespace Prototype.Utility;

public interface IAuthenticatedUserAccessor
{
    Task<UserModel?> GetUserAsync(ClaimsPrincipal user);
}