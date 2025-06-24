using Microsoft.AspNetCore.SignalR;
using Prototype.DTOs.BulkUpload;
using Prototype.Hubs;

namespace Prototype.Services.BulkUpload
{
    public class ProgressService : IProgressService
    {
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly ILogger<ProgressService> _logger;

        public ProgressService(IHubContext<ProgressHub> hubContext, ILogger<ProgressService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyJobStarted(string jobId, JobStartDto jobStart)
        {
            try
            {
                _logger.LogInformation("Notifying job started: {JobId}", jobId);
                await _hubContext.Clients.Group($"progress_{jobId}")
                    .SendAsync("JobStarted", jobStart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying job started for {JobId}", jobId);
            }
        }

        public async Task NotifyProgress(string jobId, ProgressUpdateDto progress)
        {
            try
            {
                _logger.LogInformation("Notifying progress: {JobId} - {Progress}% - {Status}", jobId, progress.ProgressPercentage, progress.Status);
                await _hubContext.Clients.Group($"progress_{jobId}")
                    .SendAsync("ProgressUpdate", progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying progress for {JobId}", jobId);
            }
        }

        public async Task NotifyJobCompleted(string jobId, JobCompleteDto jobComplete)
        {
            try
            {
                _logger.LogInformation("Notifying job completed: {JobId}", jobId);
                await _hubContext.Clients.Group($"progress_{jobId}")
                    .SendAsync("JobCompleted", jobComplete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying job completed for {JobId}", jobId);
            }
        }

        public async Task NotifyError(string jobId, string error)
        {
            try
            {
                _logger.LogWarning("Notifying error for job {JobId}: {Error}", jobId, error);
                await _hubContext.Clients.Group($"progress_{jobId}")
                    .SendAsync("JobError", new { JobId = jobId, Error = error, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying job error for {JobId}", jobId);
            }
        }

        public string GenerateJobId()
        {
            return $"job_{Guid.NewGuid():N}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}