using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.MicrosoftSQLServer;

public class AzureAdIntegratedStrategy : IConnectionStrategy
{
    public bool CanHandle(AuthenticationTypeEnum type) => type == AuthenticationTypeEnum.AzureAdIntegrated;

    public string Build(ConnectionSourceDto source)
    {
        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"Authentication=Active Directory Integrated;TrustServerCertificate=True;";
    }

    public string Build(ApplicationConnectionModel source)
    {
        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"Authentication=Active Directory Integrated;TrustServerCertificate=True;";
    }
}