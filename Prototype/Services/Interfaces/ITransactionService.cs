using Prototype.Helpers;

namespace Prototype.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation);
        Task<Result<bool>> ExecuteInTransactionAsync(Func<Task<Result<bool>>> operation);
        Task<Result<bool>> ExecuteInTransactionWithResultAsync(Func<Task> operation);
    }
}