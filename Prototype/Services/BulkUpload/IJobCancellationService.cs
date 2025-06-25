namespace Prototype.Services.BulkUpload;

public interface IJobCancellationService
{
    CancellationTokenSource CreateJobCancellation(string jobId);
    bool CancelJob(string jobId);
    CancellationToken GetCancellationToken(string jobId);
    void RemoveJob(string jobId);
    bool IsJobCancelled(string jobId);
}