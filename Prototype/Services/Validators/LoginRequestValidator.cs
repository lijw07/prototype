using Prototype.Constants;
using Prototype.DTOs;
using Prototype.Helpers;
using System.Threading.Tasks;

namespace Prototype.Services.Validators
{
    public class LoginRequestValidator : IValidator<LoginRequestDto>
    {
        public async Task<Result<LoginRequestDto>> ValidateAsync(LoginRequestDto entity)
        {
            var errors = await GetValidationErrorsAsync(entity);
            return errors.Any() 
                ? Result<LoginRequestDto>.Failure(errors) 
                : Result<LoginRequestDto>.Success(entity);
        }

        public async Task<Result<bool>> ValidatePropertyAsync(LoginRequestDto entity, string propertyName, object value)
        {
            var errors = new List<string>();
            
            switch (propertyName)
            {
                case nameof(LoginRequestDto.Username):
                    ValidateUsername(value?.ToString(), errors);
                    break;
                case nameof(LoginRequestDto.Password):
                    ValidatePassword(value?.ToString(), errors);
                    break;
            }

            return errors.Any() 
                ? Result<bool>.Failure(errors) 
                : Result<bool>.Success(true);
        }

        public async Task<List<string>> GetValidationErrorsAsync(LoginRequestDto entity)
        {
            var errors = new List<string>();

            ValidateUsername(entity.Username, errors);
            ValidatePassword(entity.Password, errors);

            return errors;
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
        }
    }
}