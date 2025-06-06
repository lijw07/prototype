namespace Prototype.Services;

/// <summary>
/// IEntitySaveService Is responsible for performing an Async operation on an entity and Saving into the Database.
/// </summary>
public interface IEntitySaveService<T> where T : class
{
    Task<T> CreateAsync(T entity);
}