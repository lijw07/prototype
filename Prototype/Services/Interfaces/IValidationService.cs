using Prototype.Helpers;

namespace Prototype.Services.Interfaces
{
    public interface IValidationService
    {
        Task<Result<T>> ValidateAsync<T>(T entity) where T : class;
        Task<Result<bool>> ValidatePropertyAsync<T>(T entity, string propertyName, object value) where T : class;
        Task<List<string>> GetValidationErrorsAsync<T>(T entity) where T : class;
    }
}