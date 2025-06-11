namespace Prototype.Services.Interfaces;

public interface IRepositoryService<T> where T : class
{
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<T?> GetByIdAsync(Guid id);
    IQueryable<T> Query();
}