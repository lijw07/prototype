using Prototype.DTOs.BulkUpload;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

public class BulkUploadLogger(ILogger<BulkUploadLogger> logger) : IBulkUploadLogger
{
    public void LogUploadStarted(string fileName, Guid userId, string tableType)
    {
        logger.LogInformation("Bulk upload started: File={FileName}, User={UserId}, TableType={TableType}", 
            fileName, userId, tableType);
    }

    public void LogUploadCompleted(BulkUploadResponseDto result, string fileName, TimeSpan duration)
    {
        logger.LogInformation("Bulk upload completed: File={FileName}, ProcessedRecords={ProcessedRecords}, " +
                             "ErrorCount={ErrorCount}, Success={Success}, Duration={Duration}ms", 
            fileName, result.ProcessedRecords, result.Errors?.Count ?? 0, result.Success, duration.TotalMilliseconds);
    }

    public void LogUploadFailed(string fileName, string errorMessage, int? processedRecords = null)
    {
        logger.LogError("Bulk upload failed: File={FileName}, Error={ErrorMessage}, ProcessedRecords={ProcessedRecords}", 
            fileName, errorMessage, processedRecords ?? 0);
    }

    public void LogValidationStarted(string fileName, int totalRecords)
    {
        logger.LogInformation("Validation started: File={FileName}, TotalRecords={TotalRecords}", 
            fileName, totalRecords);
    }

    public void LogValidationCompleted(string fileName, int validRecords, int totalRecords, TimeSpan duration)
    {
        var validationRate = totalRecords > 0 ? (double)validRecords / totalRecords * 100 : 0;
        logger.LogInformation("Validation completed: File={FileName}, ValidRecords={ValidRecords}, " +
                             "TotalRecords={TotalRecords}, ValidationRate={ValidationRate:F1}%, Duration={Duration}ms", 
            fileName, validRecords, totalRecords, validationRate, duration.TotalMilliseconds);
    }

    public void LogProcessingStarted(string fileName, int recordsToProcess)
    {
        logger.LogInformation("Processing started: File={FileName}, RecordsToProcess={RecordsToProcess}", 
            fileName, recordsToProcess);
    }

    public void LogProcessingCompleted(string fileName, int processedRecords, TimeSpan duration)
    {
        var recordsPerSecond = duration.TotalSeconds > 0 ? processedRecords / duration.TotalSeconds : 0;
        logger.LogInformation("Processing completed: File={FileName}, ProcessedRecords={ProcessedRecords}, " +
                             "Duration={Duration}ms, RecordsPerSecond={RecordsPerSecond:F1}", 
            fileName, processedRecords, duration.TotalMilliseconds, recordsPerSecond);
    }

    public void LogFileQueued(string fileName, string jobId)
    {
        logger.LogInformation("File queued for processing: File={FileName}, JobId={JobId}", fileName, jobId);
    }

    public void LogJobCancelled(string jobId, Guid userId)
    {
        logger.LogWarning("Job cancelled by user: JobId={JobId}, UserId={UserId}", jobId, userId);
    }

    public void LogConnectionTest(Guid? userId, string? applicationName = null)
    {
        if (userId.HasValue)
        {
            logger.LogDebug("Connection test executed: UserId={UserId}, Application={ApplicationName}", 
                userId.Value, applicationName ?? "Unknown");
        }
        else
        {
            logger.LogWarning("Connection test attempted without authentication");
        }
    }
}