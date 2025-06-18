using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Database.MicrosoftSQLServer;

public class UserPasswordStrategy : IConnectionStrategy
{
    public bool CanHandle(AuthenticationTypeEnum type) => type == AuthenticationTypeEnum.UserPassword;

    public string Build(ConnectionSourceDto source)
    {
        if (string.IsNullOrWhiteSpace(source.Username) || string.IsNullOrWhiteSpace(source.Password))
            throw new ArgumentException("Username and password are required for UserPassword auth.");
        
        var connection = $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
                         $"User Id={source.Username};Password={source.Password};TrustServerCertificate=True;";
        return connection;
    }

    public string Build(ApplicationConnectionModel source)
    {
        if (string.IsNullOrWhiteSpace(source.Username) || string.IsNullOrWhiteSpace(source.Password))
            throw new ArgumentException("Username and password are required for UserPassword auth.");

        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"User Id={source.Username};Password={source.Password};TrustServerCertificate=True;";
    }
}