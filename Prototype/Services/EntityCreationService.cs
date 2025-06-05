using Prototype.Data;

namespace Prototype.Services;

public class EntityCreationService<T> : IEntityCreationService<T> where T : class
{
    private readonly SentinelContext _context;

    public EntityCreationService(SentinelContext context)
    {
        _context = context;
    }

    public async Task<T> CreateAsync(T entity)
    {
        _context.Set<T>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}