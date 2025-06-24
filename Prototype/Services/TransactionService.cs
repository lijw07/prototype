using Prototype.Data;
using Prototype.Helpers;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class TransactionService : ITransactionService
{
    private readonly SentinelContext _context;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(SentinelContext context, ILogger<TransactionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Interface implementation with Result pattern
    public async Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            if (result.IsSuccess)
            {
                await _context.SaveChangesAsync();
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
            _logger.LogError(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync();
            return Result<T>.Failure($"Transaction failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ExecuteInTransactionAsync(Func<Task<Result<bool>>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            if (result.IsSuccess)
            {
                await _context.SaveChangesAsync();
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
            _logger.LogError(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync();
            return Result<bool>.Failure($"Transaction failed: {ex.Message}");
        }
    }

    public async Task<Result<bool>> ExecuteInTransactionWithResultAsync(Func<Task> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction rolled back due to error");
            await transaction.RollbackAsync();
            return Result<bool>.Failure($"Transaction failed: {ex.Message}");
        }
    }
}