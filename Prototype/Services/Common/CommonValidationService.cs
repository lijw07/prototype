using System.Text.RegularExpressions;
using Prototype.Common;

namespace Prototype.Services.Common;

public static class CommonValidationService
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[\d\s\-\(\)]+$", RegexOptions.Compiled);
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);

    public static Result<string> ValidateEmail(string? email, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return isRequired 
                ? Result<string>.Failure("Email is required")
                : Result<string>.Success(string.Empty);
        }

        if (email.Length > 254)
            return Result<string>.Failure("Email address is too long (max 254 characters)");

        if (!EmailRegex.IsMatch(email))
            return Result<string>.Failure("Invalid email format");

        return Result<string>.Success(email.Trim().ToLowerInvariant());
    }

    public static Result<string> ValidateUsername(string? username, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return isRequired 
                ? Result<string>.Failure("Username is required")
                : Result<string>.Success(string.Empty);
        }

        if (!UsernameRegex.IsMatch(username))
            return Result<string>.Failure("Username must be 3-20 characters and contain only letters, numbers, and underscores");

        return Result<string>.Success(username.Trim());
    }

    public static Result<string> ValidateStringLength(string? value, string fieldName, int minLength = 0, int maxLength = int.MaxValue, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return isRequired 
                ? Result<string>.Failure($"{fieldName} is required")
                : Result<string>.Success(string.Empty);
        }

        var trimmedValue = value.Trim();

        if (trimmedValue.Length < minLength)
            return Result<string>.Failure($"{fieldName} must be at least {minLength} characters long");

        if (trimmedValue.Length > maxLength)
            return Result<string>.Failure($"{fieldName} must not exceed {maxLength} characters");

        return Result<string>.Success(trimmedValue);
    }

    public static Result<string> ValidatePhoneNumber(string? phoneNumber, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return isRequired 
                ? Result<string>.Failure("Phone number is required")
                : Result<string>.Success(string.Empty);
        }

        var cleaned = phoneNumber.Trim();

        if (cleaned.Length < 10 || cleaned.Length > 20)
            return Result<string>.Failure("Phone number must be between 10 and 20 characters");

        if (!PhoneRegex.IsMatch(cleaned))
            return Result<string>.Failure("Invalid phone number format");

        return Result<string>.Success(cleaned);
    }

    public static Result<string> ValidatePassword(string? password, bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return isRequired 
                ? Result<string>.Failure("Password is required")
                : Result<string>.Success(string.Empty);
        }

        if (password.Length < 8)
            return Result<string>.Failure("Password must be at least 8 characters long");

        if (password.Length > 128)
            return Result<string>.Failure("Password must not exceed 128 characters");

        // Check for at least one uppercase, one lowercase, one digit
        if (!password.Any(char.IsUpper))
            return Result<string>.Failure("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            return Result<string>.Failure("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            return Result<string>.Failure("Password must contain at least one number");

        return Result<string>.Success(password);
    }

    public static Result<Guid> ValidateGuid(string? guidString, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(guidString))
            return Result<Guid>.Failure($"{fieldName} is required");

        if (!Guid.TryParse(guidString, out var guid))
            return Result<Guid>.Failure($"Invalid {fieldName} format");

        if (guid == Guid.Empty)
            return Result<Guid>.Failure($"{fieldName} cannot be empty");

        return Result<Guid>.Success(guid);
    }

    public static Result<int> ValidatePaginationParameters(int page, int pageSize, int maxPageSize = 100)
    {
        if (page < 1)
            return Result<int>.Failure("Page number must be greater than 0");

        if (pageSize < 1)
            return Result<int>.Failure("Page size must be greater than 0");

        if (pageSize > maxPageSize)
            return Result<int>.Failure($"Page size cannot exceed {maxPageSize}");

        return Result<int>.Success(pageSize);
    }

    public static Result<List<string>> ValidateStringList(IEnumerable<string>? items, string fieldName, int maxItems = 100, bool allowEmpty = false)
    {
        if (items == null)
        {
            return allowEmpty 
                ? Result<List<string>>.Success(new List<string>())
                : Result<List<string>>.Failure($"{fieldName} is required");
        }

        var itemList = items.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList();

        if (!allowEmpty && !itemList.Any())
            return Result<List<string>>.Failure($"{fieldName} cannot be empty");

        if (itemList.Count > maxItems)
            return Result<List<string>>.Failure($"{fieldName} cannot exceed {maxItems} items");

        return Result<List<string>>.Success(itemList);
    }

    public static Result<T> ValidateEnum<T>(string? value, string fieldName) where T : struct, System.Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<T>.Failure($"{fieldName} is required");

        if (!Enum.TryParse<T>(value, true, out var result))
        {
            var validValues = string.Join(", ", Enum.GetNames(typeof(T)));
            return Result<T>.Failure($"Invalid {fieldName}. Valid values are: {validValues}");
        }

        return Result<T>.Success(result);
    }

    public static Result<DateTime> ValidateDateRange(DateTime? date, string fieldName, DateTime? minDate = null, DateTime? maxDate = null)
    {
        if (!date.HasValue)
            return Result<DateTime>.Failure($"{fieldName} is required");

        if (minDate.HasValue && date < minDate)
            return Result<DateTime>.Failure($"{fieldName} cannot be earlier than {minDate:yyyy-MM-dd}");

        if (maxDate.HasValue && date > maxDate)
            return Result<DateTime>.Failure($"{fieldName} cannot be later than {maxDate:yyyy-MM-dd}");

        return Result<DateTime>.Success(date.Value);
    }
}