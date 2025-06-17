using Prototype.DTOs;

namespace Prototype.Data.Interface;

public interface IDatabaseConnectionValidator
{
    Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source);
}