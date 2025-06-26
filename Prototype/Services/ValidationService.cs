using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Prototype.Common;
using Prototype.DTOs;
using Prototype.DTOs.Request;
using Prototype.Enum;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class ValidationService : IValidationService
{
    private readonly Regex _emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private readonly Regex _phoneRegex = new(@"^\+?[\d\s\-\(\)]{10,}$", RegexOptions.Compiled);
    private readonly Regex _passwordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", RegexOptions.Compiled);
    private readonly Regex _usernameRegex = new(@"^[a-zA-Z0-9_.-]+$", RegexOptions.Compiled);

    public Result ValidateRegisterRequest(RegisterRequestDto request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required");
        else if (request.Username.Length < 3 || request.Username.Length > 100)
            errors.Add("Username must be between 3 and 100 characters");
        else if (!_usernameRegex.IsMatch(request.Username))
            errors.Add("Username can only contain letters, numbers, underscores, dots, and hyphens");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required");
        else if (!_passwordRegex.IsMatch(request.Password))
            errors.Add("Password must be at least 8 characters with uppercase, lowercase, number, and special character");

        if (request.Password != request.ReEnterPassword)
            errors.Add("Passwords do not match");

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add("Email is required");
        else if (!_emailRegex.IsMatch(request.Email))
            errors.Add("Invalid email format");

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("First name is required");
        else if (request.FirstName.Length > 50)
            errors.Add("First name cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Last name is required");
        else if (request.LastName.Length > 50)
            errors.Add("Last name cannot exceed 50 characters");

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            errors.Add("Phone number is required");
        else if (!_phoneRegex.IsMatch(request.PhoneNumber))
            errors.Add("Invalid phone number format");

        return errors.Count == 0 ? Common.Result.Success() : Common.Result.Failure(errors);
    }

    public Result ValidateLoginRequest(LoginRequestDto request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required");
        else if (request.Username.Length < 3 || request.Username.Length > 100)
            errors.Add("Username must be between 3 and 100 characters");

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required");
        else if (request.Password.Length < 8)
            errors.Add("Password must be at least 8 characters");

        return errors.Count == 0 ? Common.Result.Success() : Common.Result.Failure(errors);
    }

    public Result ValidateApplicationRequest(ApplicationRequestDto request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ApplicationName))
            errors.Add("Application name is required");
        else if (request.ApplicationName.Length > 100)
            errors.Add("Application name cannot exceed 100 characters");

        if (!string.IsNullOrEmpty(request.ApplicationDescription) && request.ApplicationDescription.Length > 500)
            errors.Add("Application description cannot exceed 500 characters");

        // Validate data source type is supported
        if (!System.Enum.IsDefined(typeof(DataSourceTypeEnum), request.DataSourceType))
            errors.Add("Invalid data source type");

        if (request.ConnectionSourceRequest != null)
        {
            var connectionValidation = ValidateConnectionSource(request.ConnectionSourceRequest);
            if (!connectionValidation.IsSuccess)
                errors.AddRange(connectionValidation.Errors);
        }
        else
        {
            errors.Add("Connection source is required");
        }

        return errors.Count == 0 ? Common.Result.Success() : Common.Result.Failure(errors);
    }

    public Result ValidateConnectionSource(ConnectionSourceRequestDto request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Host))
            errors.Add("Host is required");
        else if (request.Host.Length > 255)
            errors.Add("Host cannot exceed 255 characters");

        if (string.IsNullOrWhiteSpace(request.Port))
            errors.Add("Port is required");
        else if (!int.TryParse(request.Port, out var port) || port <= 0 || port > 65535)
            errors.Add("Port must be a valid number between 1 and 65535");

        if (!System.Enum.IsDefined(typeof(AuthenticationTypeEnum), request.AuthenticationType))
            errors.Add("Invalid authentication type");

        // Validate authentication-specific requirements
        ValidateAuthenticationSpecificFields(request, errors);

        if (!string.IsNullOrWhiteSpace(request.Url) && !Uri.IsWellFormedUriString(request.Url, UriKind.Absolute))
            errors.Add("Invalid URL format");

        return errors.Count == 0 ? Common.Result.Success() : Common.Result.Failure(errors);
    }

    private static void ValidateAuthenticationSpecificFields(ConnectionSourceRequestDto request, List<string> errors)
    {
        var requiresUsername = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.AzureAdInteractive,
            AuthenticationTypeEnum.AzureAdMsi,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256
        }.Contains(request.AuthenticationType);

        var requiresPassword = new[]
        {
            AuthenticationTypeEnum.UserPassword,
            AuthenticationTypeEnum.Kerberos,
            AuthenticationTypeEnum.AzureAdPassword,
            AuthenticationTypeEnum.PlainLdap,
            AuthenticationTypeEnum.ScramSha1,
            AuthenticationTypeEnum.ScramSha256
        }.Contains(request.AuthenticationType);

        if (requiresUsername && string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required for this authentication type");

        if (requiresPassword && string.IsNullOrWhiteSpace(request.Password))
            errors.Add("Password is required for this authentication type");

        if (request.AuthenticationType == AuthenticationTypeEnum.AwsIam)
        {
            if (string.IsNullOrWhiteSpace(request.AwsAccessKeyId))
                errors.Add("AWS Access Key ID is required for AWS IAM authentication");
            if (string.IsNullOrWhiteSpace(request.AwsSecretAccessKey))
                errors.Add("AWS Secret Access Key is required for AWS IAM authentication");
        }

        if ((request.AuthenticationType == AuthenticationTypeEnum.ScramSha1 ||
             request.AuthenticationType == AuthenticationTypeEnum.ScramSha256) &&
            string.IsNullOrWhiteSpace(request.AuthenticationDatabase))
        {
            errors.Add("Authentication Database is required for SCRAM authentication");
        }

        if (request.AuthenticationType == AuthenticationTypeEnum.GssapiKerberos)
        {
            if (string.IsNullOrWhiteSpace(request.Principal))
                errors.Add("Principal is required for GSSAPI Kerberos authentication");
            if (string.IsNullOrWhiteSpace(request.ServiceName))
                errors.Add("Service Name is required for GSSAPI Kerberos authentication");
            if (string.IsNullOrWhiteSpace(request.ServiceRealm))
                errors.Add("Service Realm is required for GSSAPI Kerberos authentication");
        }
    }

    // Interface implementation for generic validation
    public async Task<Utility.Result<T>> ValidateAsync<T>(T entity) where T : class
    {
        var errors = await GetValidationErrorsAsync(entity);
        return errors.Any() 
            ? Utility.Result<T>.Failure(errors) 
            : Utility.Result<T>.Success(entity);
    }

    public async Task<Utility.Result<bool>> ValidatePropertyAsync<T>(T entity, string propertyName, object value) where T : class
    {
        var context = new ValidationContext(entity) { MemberName = propertyName };
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateProperty(value, context, results);
        
        if (isValid)
        {
            return Utility.Result<bool>.Success(true);
        }
        
        var errors = results.Select(r => r.ErrorMessage ?? "Validation error").ToList();
        return Utility.Result<bool>.Failure(errors);
    }

    public async Task<List<string>> GetValidationErrorsAsync<T>(T entity) where T : class
    {
        var context = new ValidationContext(entity);
        var results = new List<ValidationResult>();
        
        Validator.TryValidateObject(entity, context, results, true);
        
        return results.Select(r => r.ErrorMessage ?? "Validation error").ToList();
    }
}