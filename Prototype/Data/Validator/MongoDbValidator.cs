using Prototype.Data.Interface;
using Prototype.Data.Strategy.Mongodb;
using Prototype.DTOs;

namespace Prototype.Data.Validator;

public class MongoDbValidator(MongodbAuthStrategySelector selector) : IDatabaseTypeSpecificValidator
{
    public async Task<(bool success, string message)> TestConnectionAsync(ApplicationRequestDto source)
    {
        var strategy = selector.GetStrategy(source.ConnectionSource.AuthenticationType);
        return await strategy.ConnectAsync(source);
    }
}