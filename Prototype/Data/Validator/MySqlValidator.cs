using Prototype.Data.Interface;
using Prototype.DTOs;

namespace Prototype.Data.Validator;

public class MySqlValidator : IDatabaseTypeSpecificValidator
{
    public Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source)
    {
        throw new NotImplementedException();
    }
}