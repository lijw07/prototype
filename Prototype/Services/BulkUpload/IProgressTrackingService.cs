using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload;

public interface IProgressTrackingService
{
    Task StartJobAsync(FileProcessingContext context);
    Task UpdateProgressAsync(string jobId, int progressPercentage, string message);
    Task CompleteJobAsync(string jobId, object result);
    Task CompleteJobWithErrorAsync(string jobId, string errorMessage);
}

public class ProgressTrackingService : IProgressTrackingService
{
    private readonly ILogger<ProgressTrackingService> _logger;

    public ProgressTrackingService(ILogger<ProgressTrackingService> logger)
    {
        _logger = logger;
    }

    public async Task StartJobAsync(FileProcessingContext context)
    {
        _logger.LogInformation("Starting job {JobId} for user {UserId}: {FileName}", 
            context.JobId, context.UserId, context.FileName);
    }

    public async Task UpdateProgressAsync(string jobId, int progressPercentage, string message)
    {
        _logger.LogDebug("Job {JobId} progress: {Progress}% - {Message}", 
            jobId, progressPercentage, message);
    }

    public async Task CompleteJobAsync(string jobId, object result)
    {
        _logger.LogInformation("Job {JobId} completed successfully", jobId);
    }

    public async Task CompleteJobWithErrorAsync(string jobId, string errorMessage)
    {
        _logger.LogError("Job {JobId} failed: {Error}", jobId, errorMessage);
    }
}