using Prototype.Data;

namespace Prototype.Services;

public class EntitySaveService<T>(SentinelContext context) : IEntitySaveService<T>
    where T : class
{
    public async Task<T> CreateAsync(T entity)
    {
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }
}