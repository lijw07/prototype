using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Prototype.Common;
using Prototype.Models;

namespace Prototype.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly SentinelContext Context;
    protected readonly DbSet<T> DbSet;
    protected readonly ILogger<GenericRepository<T>> Logger;

    public GenericRepository(SentinelContext context, ILogger<GenericRepository<T>> logger)
    {
        Context = context;
        DbSet = context.Set<T>();
        Logger = logger;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting {EntityType} by ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.FirstOrDefaultAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting first {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all {EntityType} entities", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.Where(predicate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting {EntityType} entities with predicate", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<PaginatedResult<T>> GetPaginatedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, object>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = DbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            var totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
            {
                query = orderByDescending 
                    ? query.OrderByDescending(orderBy)
                    : query.OrderBy(orderBy);
            }

            var skip = (page - 1) * pageSize;
            var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

            return new PaginatedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting paginated {EntityType} entities", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return predicate == null 
                ? await DbSet.CountAsync(cancellationToken)
                : await DbSet.CountAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting {EntityType} entities", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await DbSet.AnyAsync(predicate, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking existence of {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await DbSet.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding {EntityType} entity", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<List<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            await DbSet.AddRangeAsync(entityList, cancellationToken);
            return entityList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding range of {EntityType} entities", typeof(T).Name);
            throw;
        }
    }

    public virtual Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = DbSet.Update(entity);
            return Task.FromResult(entry.Entity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating {EntityType} entity", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;

            DbSet.Remove(entity);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting {EntityType} by ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    public virtual Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            DbSet.Remove(entity);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting {EntityType} entity", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
            DbSet.RemoveRange(entities);
            return entities.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting {EntityType} entities with predicate", typeof(T).Name);
            throw;
        }
    }

    public virtual async Task<Result<int>> BulkInsertAsync(IEnumerable<T> entities, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            var totalInserted = 0;

            for (int i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                await DbSet.AddRangeAsync(batch, cancellationToken);
                await Context.SaveChangesAsync(cancellationToken);
                totalInserted += batch.Count;

                Logger.LogDebug("Bulk inserted batch of {BatchSize} {EntityType} entities. Total: {Total}", 
                    batch.Count, typeof(T).Name, totalInserted);
            }

            return Result<int>.Success(totalInserted);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during bulk insert of {EntityType} entities", typeof(T).Name);
            return Result<int>.Failure($"Bulk insert failed: {ex.Message}");
        }
    }

    public virtual async Task<Result<int>> BulkUpdateAsync(IEnumerable<T> entities, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            var totalUpdated = 0;

            for (int i = 0; i < entityList.Count; i += batchSize)
            {
                var batch = entityList.Skip(i).Take(batchSize).ToList();
                DbSet.UpdateRange(batch);
                await Context.SaveChangesAsync(cancellationToken);
                totalUpdated += batch.Count;

                Logger.LogDebug("Bulk updated batch of {BatchSize} {EntityType} entities. Total: {Total}", 
                    batch.Count, typeof(T).Name, totalUpdated);
            }

            return Result<int>.Success(totalUpdated);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during bulk update of {EntityType} entities", typeof(T).Name);
            return Result<int>.Failure($"Bulk update failed: {ex.Message}");
        }
    }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving changes for {EntityType}", typeof(T).Name);
            throw;
        }
    }
}