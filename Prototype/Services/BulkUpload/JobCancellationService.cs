using System.Collections.Concurrent;

namespace Prototype.Services.BulkUpload
{
    public interface IJobCancellationService
    {
        CancellationTokenSource CreateJobCancellation(string jobId);
        bool CancelJob(string jobId);
        CancellationToken GetCancellationToken(string jobId);
        void RemoveJob(string jobId);
        bool IsJobCancelled(string jobId);
    }

    public class JobCancellationService : IJobCancellationService
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeJobs = new();
        private readonly ILogger<JobCancellationService> _logger;

        public JobCancellationService(ILogger<JobCancellationService> logger)
        {
            _logger = logger;
        }

        public CancellationTokenSource CreateJobCancellation(string jobId)
        {
            var cts = new CancellationTokenSource();
            _activeJobs.TryAdd(jobId, cts);
            _logger.LogInformation("Created cancellation token for job {JobId}", jobId);
            return cts;
        }

        public bool CancelJob(string jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var cts))
            {
                try
                {
                    _logger.LogWarning("🚨 CANCELLING job {JobId} - requesting cancellation now", jobId);
                    cts.Cancel();
                    _logger.LogWarning("🚨 CANCELLED job {JobId} - cancellation token triggered", jobId);
                    return true;
                }
                catch (ObjectDisposedException)
                {
                    // Token was already disposed
                    _logger.LogWarning("Attempted to cancel already disposed job {JobId}", jobId);
                    return false;
                }
            }

            _logger.LogWarning("⚠️ Attempted to cancel non-existent job {JobId} - job not found in active jobs", jobId);
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
                _logger.LogInformation("Removed job {JobId}", jobId);
            }
        }

        public bool IsJobCancelled(string jobId)
        {
            return _activeJobs.TryGetValue(jobId, out var cts) && cts.Token.IsCancellationRequested;
        }
    }
}