using Prototype.Exceptions;

namespace Prototype.Services.Common;

public interface IRetryPolicyService
{
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryPolicy? policy = null,
        string? operationName = null);
    
    Task ExecuteWithRetryAsync(
        Func<Task> operation,
        RetryPolicy? policy = null,
        string? operationName = null);
}

public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    public List<Type> RetryableExceptions { get; set; } = new()
    {
        typeof(ExternalServiceException),
        typeof(TimeoutException),
        typeof(DatabaseException)
    };
    
    public static RetryPolicy Default => new();
    
    public static RetryPolicy DatabaseOperation => new()
    {
        MaxRetries = 5,
        BaseDelay = TimeSpan.FromMilliseconds(500),
        BackoffMultiplier = 1.5,
        RetryableExceptions = new List<Type>
        {
            typeof(DatabaseException),
            typeof(TimeoutException)
        }
    };
    
    public static RetryPolicy ExternalService => new()
    {
        MaxRetries = 3,
        BaseDelay = TimeSpan.FromSeconds(2),
        BackoffMultiplier = 2.0,
        RetryableExceptions = new List<Type>
        {
            typeof(ExternalServiceException),
            typeof(TimeoutException),
            typeof(HttpRequestException)
        }
    };
}

public class RetryPolicyService : IRetryPolicyService
{
    private readonly ILogger<RetryPolicyService> _logger;

    public RetryPolicyService(ILogger<RetryPolicyService> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryPolicy? policy = null,
        string? operationName = null)
    {
        policy ??= RetryPolicy.Default;
        operationName ??= "Unknown Operation";
        
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= policy.MaxRetries)
        {
            try
            {
                var result = await operation();
                
                if (attempt > 0)
                {
                    _logger.LogInformation(
                        "Operation {OperationName} succeeded on attempt {Attempt}",
                        operationName, attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attempt++;

                if (attempt > policy.MaxRetries || !IsRetryableException(ex, policy))
                {
                    _logger.LogError(ex,
                        "Operation {OperationName} failed permanently after {Attempts} attempts",
                        operationName, attempt);
                    throw;
                }

                var delay = CalculateDelay(attempt, policy);
                
                _logger.LogWarning(ex,
                    "Operation {OperationName} failed on attempt {Attempt}. Retrying in {Delay}ms. Error: {Error}",
                    operationName, attempt, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay);
            }
        }

        // This should never be reached, but just in case
        throw lastException ?? new InvalidOperationException("Retry operation failed unexpectedly");
    }

    public async Task ExecuteWithRetryAsync(
        Func<Task> operation,
        RetryPolicy? policy = null,
        string? operationName = null)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, policy, operationName);
    }

    private static bool IsRetryableException(Exception ex, RetryPolicy policy)
    {
        var exceptionType = ex.GetType();
        
        return policy.RetryableExceptions.Any(retryableType =>
            retryableType.IsAssignableFrom(exceptionType));
    }

    private static TimeSpan CalculateDelay(int attempt, RetryPolicy policy)
    {
        // Exponential backoff with jitter
        var baseDelayMs = policy.BaseDelay.TotalMilliseconds;
        var exponentialDelay = baseDelayMs * Math.Pow(policy.BackoffMultiplier, attempt - 1);
        
        // Add jitter (±25%) to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * 0.5 + 0.75; // 0.75 to 1.25
        var delayMs = exponentialDelay * jitter;
        
        // Cap at max delay
        var cappedDelayMs = Math.Min(delayMs, policy.MaxDelay.TotalMilliseconds);
        
        return TimeSpan.FromMilliseconds(cappedDelayMs);
    }
}

// Extension methods for easy usage
public static class RetryPolicyExtensions
{
    public static async Task<T> WithRetryAsync<T>(
        this Task<T> task,
        IRetryPolicyService retryService,
        RetryPolicy? policy = null,
        string? operationName = null)
    {
        return await retryService.ExecuteWithRetryAsync(() => task, policy, operationName);
    }
    
    public static async Task WithRetryAsync(
        this Task task,
        IRetryPolicyService retryService,
        RetryPolicy? policy = null,
        string? operationName = null)
    {
        await retryService.ExecuteWithRetryAsync(() => task, policy, operationName);
    }
}