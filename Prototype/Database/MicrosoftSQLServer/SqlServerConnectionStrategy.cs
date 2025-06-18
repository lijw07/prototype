using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Models;

namespace Prototype.Database.MicrosoftSQLServer;

public class SqlServerConnectionStrategy(IEnumerable<IConnectionStrategy> strategies)
{
    public string Build(ConnectionSourceDto source)
    {
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(source.AuthenticationType));
        if (strategy == null)
            throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported.");

        return strategy.Build(source);
    }
    
    public string Build(ApplicationConnectionModel source)
    {
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(source.AuthenticationType));
        if (strategy == null)
            throw new NotSupportedException($"Authentication type '{source.AuthenticationType}' is not supported.");

        return strategy.Build(source);
    }
}