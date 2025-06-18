using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.MicrosoftSQLServer;

public class AzureAdPasswordStrategy : IConnectionStrategy
{
    public bool CanHandle(AuthenticationTypeEnum type) => type == AuthenticationTypeEnum.AzureAdPassword;

    public string Build(ConnectionSourceDto source)
    {
        if (string.IsNullOrWhiteSpace(source.Username) || string.IsNullOrWhiteSpace(source.Password))
            throw new ArgumentException("Username and password required for Azure AD Password.");

        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"User Id={source.Username};Password={source.Password};" +
               $"Authentication=Active Directory Password;TrustServerCertificate=True;";
    }

    public string Build(ApplicationConnectionModel source)
    {
        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"User Id={source.Username};Password={source.Password};" +
               $"Authentication=Active Directory Password;TrustServerCertificate=True;";
    }
}