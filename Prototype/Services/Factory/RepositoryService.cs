using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class RepositoryService<T>(SentinelContext context) : IRepositoryService<T>
    where T : class
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}