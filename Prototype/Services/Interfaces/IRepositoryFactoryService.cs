namespace Prototype.Services.Interfaces;

public interface IRepositoryFactoryService<T> where T : class
{
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<T?> GetByIdAsync(Guid id);
    IQueryable<T> Query(); 
}