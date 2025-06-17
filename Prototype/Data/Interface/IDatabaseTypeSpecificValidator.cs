using Prototype.DTOs;

namespace Prototype.Data.Interface;

public interface IDatabaseTypeSpecificValidator
{
    Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source);
}