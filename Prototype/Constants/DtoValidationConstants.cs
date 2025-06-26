using Prototype.Constants;

namespace Prototype.Constants;

/// <summary>
/// Constants for DTO validation attributes to ensure consistency across DTOs
/// </summary>
public static class DtoValidationConstants
{
    /// <summary>
    /// Standard field length constraints for DTOs
    /// </summary>
    public static class FieldLengths
    {
        public const int FirstNameMax = 50;
        public const int FirstNameMin = 1;
        
        public const int LastNameMax = 50;
        public const int LastNameMin = 1;
        
        public const int UsernameMax = ValidationConstants.MaxUsernameLength;
        public const int UsernameMin = ValidationConstants.MinUsernameLength;
        
        public const int EmailMax = ValidationConstants.MaxEmailLength;
        
        public const int PhoneNumberMax = 20;
        
        public const int PasswordMax = ValidationConstants.MaxPasswordLength;
        public const int PasswordMin = ValidationConstants.MinPasswordLength;
        
        public const int DescriptionMax = 500;
        public const int ApplicationNameMax = 100;
        public const int ApplicationNameMin = 2;
    }

    /// <summary>
    /// Validation attributes for consistent DTO validation
    /// </summary>
    public static class ValidationAttributes
    {
        // Regular expressions from ValidationConstants
        public const string EmailRegex = ValidationConstants.RegexPatterns.Email;
        public const string UsernameRegex = ValidationConstants.RegexPatterns.Username;
        public const string StrongPasswordRegex = ValidationConstants.RegexPatterns.StrongPassword;
        public const string PhoneNumberRegex = ValidationConstants.RegexPatterns.PhoneNumber;
        
        // Error messages from ValidationConstants
        public const string RequiredFieldMessage = ValidationConstants.ErrorMessages.RequiredField;
        public const string InvalidEmailMessage = ValidationConstants.ErrorMessages.InvalidEmail;
        public const string InvalidUsernameMessage = ValidationConstants.ErrorMessages.InvalidUsername;
        public const string WeakPasswordMessage = ValidationConstants.ErrorMessages.WeakPassword;
        public const string PasswordTooShortMessage = ValidationConstants.ErrorMessages.PasswordTooShort;
        public const string PasswordTooLongMessage = ValidationConstants.ErrorMessages.PasswordTooLong;
        public const string UsernameTooShortMessage = ValidationConstants.ErrorMessages.UsernameTooShort;
        public const string UsernameTooLongMessage = ValidationConstants.ErrorMessages.UsernameTooLong;
    }
}