using Prototype.DTOs;
using Prototype.Services;
using Xunit;

namespace Prototype.Tests.Services;

public class ValidationServiceTest
{
    private readonly ValidationService _validationService;

    public ValidationServiceTest()
    {
        _validationService = new ValidationService();
    }

    [Fact]
    public void ValidateRegisterRequest_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithEmptyUsername_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is required", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithShortUsername_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "ab",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username must be between 3 and 100 characters", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithInvalidUsernameCharacters_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "test@user",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username can only contain letters, numbers, underscores, dots, and hyphens", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithWeakPassword_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "password",
            ReEnterPassword = "password",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Password must be at least 8 characters with uppercase, lowercase, number, and special character", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithMismatchedPasswords_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "DifferentPassword123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Passwords do not match", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithInvalidEmail_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "invalid-email",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid email format", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithEmptyFirstName_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("First name is required", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithLongFirstName_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = new string('A', 51), // 51 characters
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("First name cannot exceed 50 characters", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithEmptyLastName_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Last name is required", result.Errors);
    }

    [Fact]
    public void ValidateRegisterRequest_WithEmptyPhoneNumber_ShouldReturnError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = ""
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Phone number is required", result.Errors);
    }

    [Theory]
    [InlineData("testuser")]
    [InlineData("test_user")]
    [InlineData("test.user")]
    [InlineData("test-user")]
    [InlineData("testuser123")]
    [InlineData("TestUser")]
    public void ValidateRegisterRequest_WithValidUsernames_ShouldPass(string username)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = username,
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("Password123!")]
    [InlineData("MyP@ssw0rd")]
    [InlineData("Complex123$")]
    [InlineData("Str0ng&Pass")]
    public void ValidateRegisterRequest_WithValidPasswords_ShouldPass(string password)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = password,
            ReEnterPassword = password,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("firstname+lastname@company.co.uk")]
    [InlineData("user123@test-domain.com")]
    public void ValidateRegisterRequest_WithValidEmails_ShouldPass(string email)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = email,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123-456-7890"
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("123-456-7890")]
    [InlineData("(123) 456-7890")]
    [InlineData("+1-123-456-7890")]
    [InlineData("123 456 7890")]
    [InlineData("1234567890")]
    public void ValidateRegisterRequest_WithValidPhoneNumbers_ShouldPass(string phoneNumber)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser123",
            Password = "Password123!",
            ReEnterPassword = "Password123!",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = phoneNumber
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateRegisterRequest_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "",
            Password = "weak",
            ReEnterPassword = "different",
            Email = "invalid-email",
            FirstName = "",
            LastName = "",
            PhoneNumber = ""
        };

        // Act
        var result = _validationService.ValidateRegisterRequest(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Username is required", result.Errors);
        Assert.Contains("Password must be at least 8 characters with uppercase, lowercase, number, and special character", result.Errors);
        Assert.Contains("Passwords do not match", result.Errors);
        Assert.Contains("Invalid email format", result.Errors);
        Assert.Contains("First name is required", result.Errors);
        Assert.Contains("Last name is required", result.Errors);
        Assert.Contains("Phone number is required", result.Errors);
    }
}