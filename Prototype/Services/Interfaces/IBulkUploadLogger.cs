using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.Interfaces;

public interface IBulkUploadLogger
{
    void LogUploadStarted(string fileName, Guid userId, string tableType);
    void LogUploadCompleted(BulkUploadResponseDto result, string fileName, TimeSpan duration);
    void LogUploadFailed(string fileName, string errorMessage, int? processedRecords = null);
    void LogValidationStarted(string fileName, int totalRecords);
    void LogValidationCompleted(string fileName, int validRecords, int totalRecords, TimeSpan duration);
    void LogProcessingStarted(string fileName, int recordsToProcess);
    void LogProcessingCompleted(string fileName, int processedRecords, TimeSpan duration);
    void LogFileQueued(string fileName, string jobId);
    void LogJobCancelled(string jobId, Guid userId);
    void LogConnectionTest(Guid? userId, string? applicationName = null);
}