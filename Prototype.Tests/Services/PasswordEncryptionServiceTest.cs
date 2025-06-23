using Microsoft.Extensions.Configuration;
using Moq;
using Prototype.Services;
using Xunit;

namespace Prototype.Tests.Services;

public class PasswordEncryptionServiceTest
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PasswordEncryptionService _passwordService;

    public PasswordEncryptionServiceTest()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup configuration to return a test encryption key
        _mockConfiguration.Setup(x => x["Encryption:Key"])
            .Returns("TestEncryptionKey123456789012345"); // 32+ characters for AES-256

        _passwordService = new PasswordEncryptionService(_mockConfiguration.Object);
    }

    [Fact]
    public void Encrypt_WithValidPlainText_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "sensitive password";

        // Act
        var encrypted = _passwordService.Encrypt(plainText);

        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEqual(plainText, encrypted);
        Assert.StartsWith("ENC:", encrypted);
        Assert.True(_passwordService.IsEncrypted(encrypted));
    }

    [Fact]
    public void Encrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var plainText = "";

        // Act
        var encrypted = _passwordService.Encrypt(plainText);

        // Assert
        Assert.Equal("", encrypted);
    }

    [Fact]
    public void Encrypt_WithNullString_ShouldReturnNull()
    {
        // Arrange
        string plainText = null;

        // Act
        var encrypted = _passwordService.Encrypt(plainText);

        // Assert
        Assert.Null(encrypted);
    }

    [Fact]
    public void Encrypt_WithAlreadyEncryptedText_ShouldReturnSameText()
    {
        // Arrange
        var plainText = "test password";
        var encrypted = _passwordService.Encrypt(plainText);

        // Act
        var reEncrypted = _passwordService.Encrypt(encrypted);

        // Assert
        Assert.Equal(encrypted, reEncrypted);
    }

    [Fact]
    public void Decrypt_WithValidEncryptedText_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "sensitive data to encrypt";
        var encrypted = _passwordService.Encrypt(plainText);

        // Act
        var decrypted = _passwordService.Decrypt(encrypted);

        // Assert
        Assert.Equal(plainText, decrypted);
    }

    [Fact]
    public void Decrypt_WithUnencryptedText_ShouldReturnSameText()
    {
        // Arrange
        var plainText = "unencrypted text";

        // Act
        var result = _passwordService.Decrypt(plainText);

        // Assert
        Assert.Equal(plainText, result);
    }

    [Fact]
    public void Decrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var plainText = "";

        // Act
        var result = _passwordService.Decrypt(plainText);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Decrypt_WithNullString_ShouldReturnNull()
    {
        // Arrange
        string encrypted = null;

        // Act
        var result = _passwordService.Decrypt(encrypted);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void IsEncrypted_WithEncryptedText_ShouldReturnTrue()
    {
        // Arrange
        var plainText = "test data";
        var encrypted = _passwordService.Encrypt(plainText);

        // Act
        var isEncrypted = _passwordService.IsEncrypted(encrypted);

        // Assert
        Assert.True(isEncrypted);
    }

    [Fact]
    public void IsEncrypted_WithPlainText_ShouldReturnFalse()
    {
        // Arrange
        var plainText = "plain text data";

        // Act
        var isEncrypted = _passwordService.IsEncrypted(plainText);

        // Assert
        Assert.False(isEncrypted);
    }

    [Fact]
    public void IsEncrypted_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var text = "";

        // Act
        var isEncrypted = _passwordService.IsEncrypted(text);

        // Assert
        Assert.False(isEncrypted);
    }

    [Fact]
    public void IsEncrypted_WithNullString_ShouldReturnFalse()
    {
        // Arrange
        string text = null;

        // Act
        var isEncrypted = _passwordService.IsEncrypted(text);

        // Assert
        Assert.False(isEncrypted);
    }

    [Fact]
    public void HashPassword_WithValidPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hashedPassword = _passwordService.HashPassword(password);

        // Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
        Assert.StartsWith("$2a$", hashedPassword); // BCrypt hash format
    }

    [Fact]
    public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // BCrypt uses salt, so same password should produce different hashes
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "MySecurePassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var isValid = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "MySecurePassword123!";
        var incorrectPassword = "WrongPassword456!";
        var hashedPassword = _passwordService.HashPassword(correctPassword);

        // Act
        var isValid = _passwordService.VerifyPassword(incorrectPassword, hashedPassword);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveOriginalData()
    {
        // Arrange
        var originalData = "This is a test message with special characters: !@#$%^&*()";

        // Act
        var encrypted = _passwordService.Encrypt(originalData);
        var decrypted = _passwordService.Decrypt(encrypted);

        // Assert
        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_ShouldWork()
    {
        // Arrange
        var longText = new string('A', 1000) + "Special content: " + new string('B', 1000);

        // Act
        var encrypted = _passwordService.Encrypt(longText);
        var decrypted = _passwordService.Decrypt(encrypted);

        // Assert
        Assert.Equal(longText, decrypted);
        Assert.True(_passwordService.IsEncrypted(encrypted));
    }

    [Fact]
    public void EncryptDecrypt_WithUnicodeCharacters_ShouldWork()
    {
        // Arrange
        var unicodeText = "Hello 世界! Café résumé naïve 🚀 emoji test";

        // Act
        var encrypted = _passwordService.Encrypt(unicodeText);
        var decrypted = _passwordService.Decrypt(encrypted);

        // Assert
        Assert.Equal(unicodeText, decrypted);
        Assert.True(_passwordService.IsEncrypted(encrypted));
    }

    [Fact]
    public void Constructor_WithMissingKeyInProduction_ShouldThrowException()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["Encryption:Key"]).Returns((string)null);
        
        // Set production environment and clear encryption key
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var originalKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", null);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => new PasswordEncryptionService(mockConfig.Object));
            Assert.Contains("Encryption key not configured", exception.Message);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("ENCRYPTION_KEY", originalKey);
        }
    }
}