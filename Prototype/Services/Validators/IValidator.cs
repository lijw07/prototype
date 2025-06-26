using Prototype.Helpers;
using System.Threading.Tasks;

namespace Prototype.Services.Validators
{
    public interface IValidator<T> where T : class
    {
        Task<Result<T>> ValidateAsync(T entity);
        Task<Result<bool>> ValidatePropertyAsync(T entity, string propertyName, object value);
        Task<List<string>> GetValidationErrorsAsync(T entity);
    }
}