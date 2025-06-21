using Prototype.Database.Interface;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Database.MicrosoftSQLServer;

public class UserPasswordStrategy : IConnectionStrategy
{
    private readonly PasswordEncryptionService _encryptionService;

    public UserPasswordStrategy(PasswordEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

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
        
        var decryptedPassword = _encryptionService.Decrypt(source.Password);

        return $"Server={source.Host},{source.Port};Database={source.DatabaseName ?? "master"};" +
               $"User Id={source.Username};Password={decryptedPassword};TrustServerCertificate=True;";
    }
}