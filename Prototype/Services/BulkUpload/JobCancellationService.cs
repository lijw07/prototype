using System.Collections.Concurrent;

namespace Prototype.Services.BulkUpload;

public class JobCancellationService(ILogger<JobCancellationService> logger) : IJobCancellationService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeJobs = new();

    public CancellationTokenSource CreateJobCancellation(string jobId)
    {
        var cts = new CancellationTokenSource();
        _activeJobs.TryAdd(jobId, cts);
        logger.LogInformation("Created cancellation token for job {JobId}", jobId);
        return cts;
    }

    public bool CancelJob(string jobId)
    {
        if (_activeJobs.TryGetValue(jobId, out var cts))
        {
            try
            {
                logger.LogWarning("🚨 CANCELLING job {JobId} - requesting cancellation now", jobId);
                cts.Cancel();
                logger.LogWarning("🚨 CANCELLED job {JobId} - cancellation token triggered", jobId);
                return true;
            }
            catch (ObjectDisposedException)
            {
                // Token was already disposed
                logger.LogWarning("Attempted to cancel already disposed job {JobId}", jobId);
                return false;
            }
        }

        logger.LogWarning("⚠️ Attempted to cancel non-existent job {JobId} - job not found in active jobs", jobId);
        return false;
    }

    public CancellationToken GetCancellationToken(string jobId)
    {
        return _activeJobs.TryGetValue(jobId, out var cts) ? cts.Token : CancellationToken.None;
    }

    public void RemoveJob(string jobId)
    {
        if (_activeJobs.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
            logger.LogInformation("Removed job {JobId}", jobId);
        }
    }

    public bool IsJobCancelled(string jobId)
    {
        return _activeJobs.TryGetValue(jobId, out var cts) && cts.Token.IsCancellationRequested;
    }
}