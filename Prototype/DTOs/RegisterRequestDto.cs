using System.ComponentModel.DataAnnotations;
using Prototype.Constants;

namespace Prototype.DTOs;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(DtoValidationConstants.FieldLengths.FirstNameMax, MinimumLength = DtoValidationConstants.FieldLengths.FirstNameMin, 
        ErrorMessage = "First name must be between 1 and 50 characters")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(DtoValidationConstants.FieldLengths.LastNameMax, MinimumLength = DtoValidationConstants.FieldLengths.LastNameMin, 
        ErrorMessage = "Last name must be between 1 and 50 characters")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Username is required")]
    [StringLength(DtoValidationConstants.FieldLengths.UsernameMax, MinimumLength = DtoValidationConstants.FieldLengths.UsernameMin, 
        ErrorMessage = DtoValidationConstants.ValidationAttributes.UsernameTooShortMessage)]
    [RegularExpression(DtoValidationConstants.ValidationAttributes.UsernameRegex, 
        ErrorMessage = DtoValidationConstants.ValidationAttributes.InvalidUsernameMessage)]
    public required string Username { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = DtoValidationConstants.ValidationAttributes.InvalidEmailMessage)]
    [StringLength(DtoValidationConstants.FieldLengths.EmailMax, ErrorMessage = "Email cannot exceed 100 characters")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(DtoValidationConstants.ValidationAttributes.PhoneNumberRegex, ErrorMessage = "Invalid phone number format")]
    [StringLength(DtoValidationConstants.FieldLengths.PhoneNumberMax, ErrorMessage = "Phone number cannot exceed 20 characters")]
    public required string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(DtoValidationConstants.FieldLengths.PasswordMax, MinimumLength = DtoValidationConstants.FieldLengths.PasswordMin, 
        ErrorMessage = DtoValidationConstants.ValidationAttributes.PasswordTooShortMessage)]
    [RegularExpression(DtoValidationConstants.ValidationAttributes.StrongPasswordRegex, 
        ErrorMessage = DtoValidationConstants.ValidationAttributes.WeakPasswordMessage)]
    public required string Password { get; set; }

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string? ReEnterPassword { get; set; }
}