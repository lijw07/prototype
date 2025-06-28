using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Helpers;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class TransactionService(SentinelContext context, ILogger<TransactionService> logger)
    : ITransactionService
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to error");
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to error");
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // Interface implementation with Result pattern
    public async Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                if (result.IsSuccess)
                {
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to error");
                await transaction.RollbackAsync();
                return Result<T>.Failure($"Transaction failed: {ex.Message}");
            }
        });
    }

    public async Task<Result<bool>> ExecuteInTransactionAsync(Func<Task<Result<bool>>> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var result = await operation();
                if (result.IsSuccess)
                {
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                else
                {
                    await transaction.RollbackAsync();
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to error");
                await transaction.RollbackAsync();
                return Result<bool>.Failure($"Transaction failed: {ex.Message}");
            }
        });
    }

    public async Task<Result<bool>> ExecuteInTransactionWithResultAsync(Func<Task> operation)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to error");
                await transaction.RollbackAsync();
                return Result<bool>.Failure($"Transaction failed: {ex.Message}");
            }
        });
    }
}