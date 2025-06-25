using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload
{
    public interface IProgressService
    {
        Task NotifyJobStarted(string jobId, JobStartDto jobStart);
        Task NotifyProgress(string jobId, ProgressUpdateDto progress);
        Task NotifyJobCompleted(string jobId, JobCompleteDto jobComplete);
        Task NotifyError(string jobId, string error);
        string GenerateJobId();
    }
}