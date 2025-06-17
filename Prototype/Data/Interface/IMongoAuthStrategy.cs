using Prototype.DTOs;

namespace Prototype.Data.Interface;

public interface IMongoAuthStrategy
{
    Task<(bool success, string message)> ConnectAsync(ApplicationRequestDto dto);
}