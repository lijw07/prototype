using BCrypt.Net;
using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Services;


public class TemporaryUserFactoryService : ITemporaryUserFactoryService
{
    public TemporaryUserModel CreateTemporaryUserFromRequest(RegisterRequest request, string verificationCode)
    {
        return new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.Now,
            VerificationCode = verificationCode
        };
    }
    
    public UserModel CreateUserFromTemporaryUser(TemporaryUserModel tempUser)
    {
        return new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = tempUser.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempUser.PasswordHash),
            FirstName = tempUser.FirstName,
            LastName = tempUser.LastName,
            Email = tempUser.Email,
            PhoneNumber = tempUser.PhoneNumber,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}