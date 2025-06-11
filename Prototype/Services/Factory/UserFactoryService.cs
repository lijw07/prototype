using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class UserFactoryService : IUserFactoryService
{
    public TemporaryUserModel CreateTemporaryUser(RegisterRequestDto requestDto, string verificationCode)
    {
        return new TemporaryUserModel
        {
            TemporaryUserId = Guid.NewGuid(),
            Username = requestDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDto.Password),
            FirstName = requestDto.FirstName,
            LastName = requestDto.LastName,
            Email = requestDto.Email,
            PhoneNumber = requestDto.PhoneNumber,
            CreatedAt = DateTime.Now,
            token = verificationCode,
        };
    }

    public UserModel CreateUserFromTemporary(TemporaryUserModel tempUser)
    {
        return new UserModel
        {
            UserId = Guid.NewGuid(),
            Username = tempUser.Username,
            PasswordHash = tempUser.PasswordHash,
            FirstName = tempUser.FirstName,
            LastName = tempUser.LastName,
            Email = tempUser.Email,
            PhoneNumber = tempUser.PhoneNumber,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }
}