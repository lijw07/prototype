using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;

namespace Prototype.Utility;

public class AuthenticatedUserAccessor(SentinelContext context) : IAuthenticatedUserAccessor
{
    public async Task<UserModel?> GetUserAsync(ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        return await context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    }
}