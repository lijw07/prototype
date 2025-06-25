using System.Collections.Concurrent;
using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload
{
    public interface IFileQueueService
    {
        Task<string> QueueMultipleFilesAsync(QueuedFileUploadRequest request);
        Task ProcessQueueAsync(string jobId, CancellationToken cancellationToken = default);
        QueueStatus GetQueueStatus(string jobId);
        List<QueuedFileInfo> GetQueuedFiles(string jobId);
        bool CancelQueue(string jobId);
    }

    public class FileQueueService : IFileQueueService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IProgressService _progressService;
        private readonly IJobCancellationService _jobCancellationService;
        private readonly ILogger<FileQueueService> _logger;
        
        private readonly ConcurrentDictionary<string, FileQueue> _activeQueues = new();

        public FileQueueService(
            IServiceScopeFactory serviceScopeFactory,
            IProgressService progressService,
            IJobCancellationService jobCancellationService,
            ILogger<FileQueueService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _progressService = progressService;
            _jobCancellationService = jobCancellationService;
            _logger = logger;
        }

        public async Task<string> QueueMultipleFilesAsync(QueuedFileUploadRequest request)
        {
            var jobId = _progressService.GenerateJobId();
            _logger.LogInformation("Creating file queue for job {JobId} with {FileCount} files", jobId, request.Files.Count);

            var queuedFiles = new List<QueuedFileInfo>();
            
            // Create a scope for table detection
            using var scope = _serviceScopeFactory.CreateScope();
            var tableDetectionService = scope.ServiceProvider.GetRequiredService<ITableDetectionService>();
            
            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                var fileData = await ReadFileDataAsync(file);
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                // Detect table type for each file
                var detectedTable = await tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                
                queuedFiles.Add(new QueuedFileInfo
                {
                    FileName = file.FileName,
                    FileData = fileData,
                    FileExtension = fileExtension,
                    TableType = detectedTable?.TableType ?? "Unknown",
                    Status = QueuedFileStatus.Queued,
                    FileIndex = i,
                    QueuedAt = DateTime.UtcNow
                });
            }

            var fileQueue = new FileQueue
            {
                JobId = jobId,
                UserId = request.UserId,
                Files = queuedFiles,
                Status = QueueStatus.Queued,
                CreatedAt = DateTime.UtcNow,
                IgnoreErrors = request.IgnoreErrors,
                ContinueOnError = request.ContinueOnError
            };

            _activeQueues.TryAdd(jobId, fileQueue);
            
            _logger.LogInformation("File queue created for job {JobId}. Starting background processing.", jobId);
            
            // Start processing in background with proper scope management
            _ = Task.Run(async () =>
            {
                try
                {
                    var cancellationTokenSource = _jobCancellationService.CreateJobCancellation(jobId);
                    await ProcessQueueAsync(jobId, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background queue processing for job {JobId}", jobId);
                }
            });

            return jobId;
        }

        public async Task ProcessQueueAsync(string jobId, CancellationToken cancellationToken = default)
        {
            if (!_activeQueues.TryGetValue(jobId, out var queue))
            {
                _logger.LogWarning("Queue not found for job {JobId}", jobId);
                return;
            }

            _logger.LogInformation("Starting queue processing for job {JobId} with {FileCount} files", jobId, queue.Files.Count);
            
            queue.Status = QueueStatus.Processing;
            queue.StartedAt = DateTime.UtcNow;

            try
            {
                // Initial progress notification
                await _progressService.NotifyJobStarted(jobId, new JobStartDto
                {
                    JobId = jobId,
                    JobType = "FileQueue",
                    TotalFiles = queue.Files.Count,
                    EstimatedTotalRecords = 0,
                    StartTime = DateTime.UtcNow
                });

                var totalFiles = queue.Files.Count;
                var completedFiles = 0;
                var failedFiles = 0;

                foreach (var queuedFile in queue.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    _logger.LogInformation("Processing file {FileName} ({FileIndex}/{TotalFiles}) for job {JobId}", 
                        queuedFile.FileName, queuedFile.FileIndex + 1, totalFiles, jobId);

                    queuedFile.Status = QueuedFileStatus.Processing;
                    queuedFile.StartedAt = DateTime.UtcNow;

                    // Notify that this file is starting
                    await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                    {
                        JobId = jobId,
                        ProgressPercentage = (double)completedFiles / totalFiles * 100,
                        Status = "Processing",
                        CurrentOperation = $"Processing file: {queuedFile.FileName}",
                        ProcessedRecords = 0,
                        TotalRecords = 0,
                        CurrentFileName = queuedFile.FileName,
                        ProcessedFiles = completedFiles,
                        TotalFiles = totalFiles
                    });

                    try
                    {
                        if (string.IsNullOrEmpty(queuedFile.TableType) || queuedFile.TableType == "Unknown")
                        {
                            throw new InvalidOperationException($"Could not determine table type for file {queuedFile.FileName}");
                        }

                        // Create a new scope for each file to avoid context disposal issues
                        using var fileScope = _serviceScopeFactory.CreateScope();
                        var bulkUploadService = fileScope.ServiceProvider.GetRequiredService<IBulkUploadService>();

                        // Process the file with progress tracking
                        var result = await bulkUploadService.ProcessBulkDataWithProgressAsync(
                            queuedFile.FileData,
                            queuedFile.TableType,
                            queuedFile.FileExtension,
                            queue.UserId,
                            jobId,
                            queuedFile.FileName,
                            queuedFile.FileIndex,
                            totalFiles,
                            queue.IgnoreErrors,
                            cancellationToken
                        );

                        if (result.IsSuccess && result.Data != null)
                        {
                            queuedFile.Status = QueuedFileStatus.Completed;
                            queuedFile.CompletedAt = DateTime.UtcNow;
                            queuedFile.ProcessedRecords = result.Data.ProcessedRecords;
                            queuedFile.FailedRecords = result.Data.FailedRecords;
                            queuedFile.TotalRecords = result.Data.TotalRecords;
                            queuedFile.ProcessingTime = result.Data.ProcessingTime;
                            queuedFile.Errors = result.Data.Errors?.Select(e => e.ErrorMessage).ToList() ?? new List<string>();

                            completedFiles++;
                            _logger.LogInformation("File {FileName} completed successfully. Processed: {ProcessedRecords}, Failed: {FailedRecords}", 
                                queuedFile.FileName, result.Data.ProcessedRecords, result.Data.FailedRecords);
                        }
                        else
                        {
                            queuedFile.Status = QueuedFileStatus.Failed;
                            queuedFile.CompletedAt = DateTime.UtcNow;
                            queuedFile.Errors = new List<string> { result.ErrorMessage ?? "Unknown error" };
                            failedFiles++;

                            if (!queue.ContinueOnError)
                            {
                                _logger.LogWarning("File {FileName} failed and ContinueOnError is false. Stopping queue processing.", queuedFile.FileName);
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Queue processing cancelled for job {JobId} at file {FileName}", jobId, queuedFile.FileName);
                        queuedFile.Status = QueuedFileStatus.Cancelled;
                        throw; // Re-throw to handle at queue level
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file {FileName} in job {JobId}", queuedFile.FileName, jobId);
                        
                        queuedFile.Status = QueuedFileStatus.Failed;
                        queuedFile.CompletedAt = DateTime.UtcNow;
                        queuedFile.Errors = new List<string> { ex.Message };
                        failedFiles++;

                        if (!queue.ContinueOnError)
                        {
                            break;
                        }
                    }
                }

                // Final completion notification
                queue.Status = failedFiles == 0 ? QueueStatus.Completed : QueueStatus.CompletedWithErrors;
                queue.CompletedAt = DateTime.UtcNow;

                await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
                {
                    JobId = jobId,
                    Success = failedFiles == 0,
                    Message = failedFiles == 0 
                        ? $"All {totalFiles} files processed successfully" 
                        : $"Processing completed. {completedFiles} succeeded, {failedFiles} failed",
                    Data = null, // We'll provide queue status through the API endpoint instead
                    CompletedAt = DateTime.UtcNow,
                    TotalDuration = DateTime.UtcNow - queue.CreatedAt
                });

                _logger.LogInformation("Queue processing completed for job {JobId}. Files: {TotalFiles}, Completed: {CompletedFiles}, Failed: {FailedFiles}", 
                    jobId, totalFiles, completedFiles, failedFiles);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Queue processing was cancelled for job {JobId}", jobId);
                
                queue.Status = QueueStatus.Cancelled;
                queue.CompletedAt = DateTime.UtcNow;

                // Mark any still-processing files as cancelled
                foreach (var file in queue.Files.Where(f => f.Status == QueuedFileStatus.Processing || f.Status == QueuedFileStatus.Queued))
                {
                    file.Status = QueuedFileStatus.Cancelled;
                    file.CompletedAt = DateTime.UtcNow;
                }

                await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
                {
                    JobId = jobId,
                    Success = false,
                    Message = "Queue processing was cancelled by user",
                    Data = null,
                    CompletedAt = DateTime.UtcNow,
                    TotalDuration = DateTime.UtcNow - queue.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue for job {JobId}", jobId);
                
                queue.Status = QueueStatus.Failed;
                queue.CompletedAt = DateTime.UtcNow;

                await _progressService.NotifyError(jobId, $"Queue processing failed: {ex.Message}");
            }
            finally
            {
                // Clean up the cancellation token
                _jobCancellationService.RemoveJob(jobId);
                
                // Remove the queue after some time to allow status checks
                _ = Task.Delay(TimeSpan.FromMinutes(30)).ContinueWith(_ =>
                {
                    _activeQueues.TryRemove(jobId, out var _);
                    _logger.LogInformation("Cleaned up queue for job {JobId}", jobId);
                });
            }
        }

        public QueueStatus GetQueueStatus(string jobId)
        {
            return _activeQueues.TryGetValue(jobId, out var queue) ? queue.Status : QueueStatus.NotFound;
        }

        public List<QueuedFileInfo> GetQueuedFiles(string jobId)
        {
            return _activeQueues.TryGetValue(jobId, out var queue) ? queue.Files : new List<QueuedFileInfo>();
        }

        public bool CancelQueue(string jobId)
        {
            if (_activeQueues.TryGetValue(jobId, out var queue))
            {
                _logger.LogInformation("Cancelling queue for job {JobId}", jobId);
                return _jobCancellationService.CancelJob(jobId);
            }
            
            _logger.LogWarning("Attempted to cancel non-existent queue for job {JobId}", jobId);
            return false;
        }

        private async Task<byte[]> ReadFileDataAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }

    // Supporting DTOs and enums
    public class QueuedFileUploadRequest
    {
        public List<IFormFile> Files { get; set; } = new();
        public Guid UserId { get; set; }
        public bool IgnoreErrors { get; set; }
        public bool ContinueOnError { get; set; } = true;
    }

    public class FileQueue
    {
        public string JobId { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public List<QueuedFileInfo> Files { get; set; } = new();
        public QueueStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IgnoreErrors { get; set; }
        public bool ContinueOnError { get; set; }
    }

    public class QueuedFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string FileExtension { get; set; } = string.Empty;
        public string TableType { get; set; } = string.Empty;
        public QueuedFileStatus Status { get; set; }
        public int FileIndex { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public int TotalRecords { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public enum QueueStatus
    {
        NotFound,
        Queued,
        Processing,
        Completed,
        CompletedWithErrors,
        Failed,
        Cancelled
    }

    public enum QueuedFileStatus
    {
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}