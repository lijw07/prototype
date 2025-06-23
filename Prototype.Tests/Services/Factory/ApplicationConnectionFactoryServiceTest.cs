using Microsoft.Extensions.Configuration;
using Moq;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Services;
using Prototype.Services.Factory;
using Xunit;

namespace Prototype.Tests.Services.Factory;

public class ApplicationConnectionFactoryServiceTest
{
    private readonly ApplicationConnectionFactoryService _factory;
    private readonly PasswordEncryptionService _passwordService;

    public ApplicationConnectionFactoryServiceTest()
    {
        // Setup encryption service
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Encryption:Key"]).Returns("test-encryption-key-1234567890123456");
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", "test-encryption-key-1234567890123456");
        
        _passwordService = new PasswordEncryptionService(mockConfig.Object);
        _factory = new ApplicationConnectionFactoryService(_passwordService);
    }

    [Fact]
    public void CreateApplicationConnection_WithValidData_ReturnsConnectionModel()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = "testpass",
            AuthenticationType = AuthenticationTypeEnum.UserPassword,
            Url = "localhost:5432",
            AuthenticationDatabase = "auth_db"
        };

        // Act
        var result = _factory.CreateApplicationConnection(applicationId, connectionDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(applicationId, result.ApplicationId);
        Assert.Equal("test-instance", result.Instance);
        Assert.Equal("localhost", result.Host);
        Assert.Equal("5432", result.Port);
        Assert.Equal("testdb", result.DatabaseName);
        Assert.Equal("testuser", result.Username);
        Assert.True(result.Password.StartsWith("ENC:"));
        Assert.Equal(AuthenticationTypeEnum.UserPassword, result.AuthenticationType);
        Assert.Equal("localhost:5432", result.Url);
        Assert.Equal("auth_db", result.AuthenticationDatabase);
        Assert.True(result.ApplicationConnectionId != Guid.Empty);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.True(result.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateApplicationConnection_WithNullPassword_EncryptsEmptyString()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = null,
            AuthenticationType = AuthenticationTypeEnum.NoAuth,
            Url = "localhost:5432"
        };

        // Act
        var result = _factory.CreateApplicationConnection(applicationId, connectionDto);

        // Assert
        Assert.Equal("", result.Password);
    }

    [Fact]
    public void CreateApplicationConnection_WithEmptyPassword_EncryptsEmptyString()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = "",
            AuthenticationType = AuthenticationTypeEnum.NoAuth,
            Url = "localhost:5432"
        };

        // Act
        var result = _factory.CreateApplicationConnection(applicationId, connectionDto);

        // Assert
        Assert.Equal("", result.Password);
    }

    [Fact]
    public void CreateApplicationConnection_SetsTimestamps()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = "testpass",
            AuthenticationType = AuthenticationTypeEnum.UserPassword,
            Url = "localhost:5432"
        };


        // Act
        var result = _factory.CreateApplicationConnection(applicationId, connectionDto);
        var after = DateTime.UtcNow;

        // Assert
        Assert.True(result.CreatedAt >= before && result.CreatedAt <= after);
        Assert.True(result.UpdatedAt >= before && result.UpdatedAt <= after);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
    }

    [Fact]
    public void CreateApplicationConnection_GeneratesUniqueId()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = "testpass",
            AuthenticationType = AuthenticationTypeEnum.UserPassword,
            Url = "localhost:5432"
        };


        // Act
        var result1 = _factory.CreateApplicationConnection(applicationId, connectionDto);
        var result2 = _factory.CreateApplicationConnection(applicationId, connectionDto);

        // Assert
        Assert.NotEqual(result1.ApplicationConnectionId, result2.ApplicationConnectionId);
        Assert.NotEqual(Guid.Empty, result1.ApplicationConnectionId);
        Assert.NotEqual(Guid.Empty, result2.ApplicationConnectionId);
    }

    [Fact]
    public void CreateApplicationConnection_WithDifferentAuthenticationTypes_SetsCorrectType()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var authTypes = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.NoAuth,
            AuthenticationTypeEnum.WindowsIntegrated
        };


        foreach (var authType in authTypes)
        {
            var connectionDto = new ConnectionSourceDto
            {
                Instance = "test-instance",
                Host = "localhost",
                Port = "5432",
                DatabaseName = "testdb",
                Username = "testuser",
                Password = "testpass",
                AuthenticationType = authType,
                Url = "localhost:5432"
            };

            // Act
            var result = _factory.CreateApplicationConnection(applicationId, connectionDto);

            // Assert
            Assert.Equal(authType, result.AuthenticationType);
        }
    }

    [Fact]
    public void CreateApplicationConnection_AlwaysEncryptsPassword()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var connectionDto = new ConnectionSourceDto
        {
            Instance = "test-instance",
            Host = "localhost",
            Port = "5432",
            DatabaseName = "testdb",
            Username = "testuser",
            Password = "plaintext_password",
            AuthenticationType = AuthenticationTypeEnum.UserPassword,
            Url = "localhost:5432"
        };

        // Act
        var result = _factory.CreateApplicationConnection(applicationId, connectionDto);

        // Assert
        Assert.True(result.Password.StartsWith("ENC:"));
    }
}