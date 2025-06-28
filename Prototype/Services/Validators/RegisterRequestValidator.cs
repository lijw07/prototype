using Prototype.Constants;
using Prototype.DTOs;
using Prototype.Helpers;
using System.Text.RegularExpressions;

namespace Prototype.Services.Validators
{
    public class RegisterRequestValidator : IValidator<RegisterRequestDto>
    {
        public async Task<Result<RegisterRequestDto>> ValidateAsync(RegisterRequestDto entity)
        {
            var errors = await GetValidationErrorsAsync(entity);
            return errors.Any() 
                ? Result<RegisterRequestDto>.Failure(errors) 
                : Result<RegisterRequestDto>.Success(entity);
        }

        public async Task<Result<bool>> ValidatePropertyAsync(RegisterRequestDto entity, string propertyName, object value)
        {
            var errors = new List<string>();
            
            switch (propertyName)
            {
                case nameof(RegisterRequestDto.FirstName):
                    ValidateFirstName(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.LastName):
                    ValidateLastName(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.Username):
                    ValidateUsername(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.Email):
                    ValidateEmail(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.PhoneNumber):
                    ValidatePhoneNumber(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.Password):
                    ValidatePassword(value?.ToString(), errors);
                    break;
                case nameof(RegisterRequestDto.ReEnterPassword):
                    ValidatePasswordConfirmation(entity.Password, value?.ToString(), errors);
                    break;
            }

            return errors.Any() 
                ? Result<bool>.Failure(errors) 
                : Result<bool>.Success(true);
        }

        public async Task<List<string>> GetValidationErrorsAsync(RegisterRequestDto entity)
        {
            var errors = new List<string>();

            ValidateFirstName(entity.FirstName, errors);
            ValidateLastName(entity.LastName, errors);
            ValidateUsername(entity.Username, errors);
            ValidateEmail(entity.Email, errors);
            ValidatePhoneNumber(entity.PhoneNumber, errors);
            ValidatePassword(entity.Password, errors);
            ValidatePasswordConfirmation(entity.Password, entity.ReEnterPassword, errors);

            return errors;
        }

        private void ValidateFirstName(string firstName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(firstName))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "First name"));
                return;
            }

            if (firstName.Length > 50)
            {
                errors.Add("First name must not exceed 50 characters");
            }
        }

        private void ValidateLastName(string lastName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(lastName))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Last name"));
                return;
            }

            if (lastName.Length > 50)
            {
                errors.Add("Last name must not exceed 50 characters");
            }
        }

        private void ValidateUsername(string username, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Username"));
                return;
            }

            if (username.Length < ValidationConstants.MinUsernameLength)
            {
                errors.Add(ValidationConstants.ErrorMessages.UsernameTooShort);
            }
            else if (username.Length > ValidationConstants.MaxUsernameLength)
            {
                errors.Add(ValidationConstants.ErrorMessages.UsernameTooLong);
            }

            if (!Regex.IsMatch(username, ValidationConstants.RegexPatterns.Username))
            {
                errors.Add(ValidationConstants.ErrorMessages.InvalidUsername);
            }
        }

        private void ValidateEmail(string email, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Email"));
                return;
            }

            if (email.Length > ValidationConstants.MaxEmailLength)
            {
                errors.Add("Email must not exceed 100 characters");
            }

            if (!Regex.IsMatch(email, ValidationConstants.RegexPatterns.Email))
            {
                errors.Add(ValidationConstants.ErrorMessages.InvalidEmail);
            }
        }

        private void ValidatePhoneNumber(string phoneNumber, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Phone number"));
                return;
            }

            if (phoneNumber.Length > 20)
            {
                errors.Add("Phone number must not exceed 20 characters");
            }

            if (!Regex.IsMatch(phoneNumber, ValidationConstants.RegexPatterns.PhoneNumber))
            {
                errors.Add("Invalid phone number format");
            }
        }

        private void ValidatePassword(string password, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Password"));
                return;
            }

            if (password.Length < ValidationConstants.MinPasswordLength)
            {
                errors.Add(ValidationConstants.ErrorMessages.PasswordTooShort);
            }
            else if (password.Length > ValidationConstants.MaxPasswordLength)
            {
                errors.Add(ValidationConstants.ErrorMessages.PasswordTooLong);
            }

            if (!Regex.IsMatch(password, ValidationConstants.RegexPatterns.StrongPassword))
            {
                errors.Add(ValidationConstants.ErrorMessages.WeakPassword);
            }
        }

        private void ValidatePasswordConfirmation(string password, string confirmPassword, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                errors.Add(string.Format(ValidationConstants.ErrorMessages.RequiredField, "Password confirmation"));
                return;
            }

            if (!string.Equals(password, confirmPassword))
            {
                errors.Add("Passwords do not match");
            }
        }
    }
}