namespace Prototype.Services;

public interface IEntityCreationService<T> where T : class
{
    Task<T> CreateAsync(T entity);
}