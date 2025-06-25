using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Services.BulkUpload;

public class FileQueueService : IFileQueueService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IProgressService _progressService;
    private readonly IJobCancellationService _jobCancellationService;
    private readonly ILogger<FileQueueService> _logger;
    
    private readonly ConcurrentDictionary<string, FileQueueRequestDto> _activeQueues = new();

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
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<string> QueueMultipleFilesAsync(QueuedFileUploadRequestDto request)
    {
        var jobId = _progressService.GenerateJobId();
        _logger.LogInformation("Creating file queue for job {JobId} with {FileCount} files", jobId, request.Files.Count);

        var queuedFiles = new List<QueuedFileInfoRequestDto>();
        
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
            
            queuedFiles.Add(new QueuedFileInfoRequestDto
            {
                FileName = file.FileName,
                FileData = fileData,
                FileExtension = fileExtension,
                TableType = detectedTable?.TableType ?? "Unknown",
                Status = QueuedFileStatusEnum.Queued,
                FileIndex = i,
                QueuedAt = DateTime.UtcNow
            });
        }

        var fileQueue = new FileQueueRequestDto
        {
            JobId = jobId,
            UserId = request.UserId,
            Files = queuedFiles,
            Status = QueueStatusEnum.Queued,
            CreatedAt = DateTime.UtcNow,
            IgnoreErrors = request.IgnoreErrors,
            ContinueOnError = request.ContinueOnError
        };

        _activeQueues.TryAdd(jobId, fileQueue);
        
        _logger.LogInformation("File queue created for job {JobId}. Starting background processing.", jobId);
        
        // Start processing in the background with proper scope management
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
        
        queue.Status = QueueStatusEnum.Processing;
        queue.StartedAt = DateTime.UtcNow;

        try
        {
            // Calculate total records across all files for initial estimate
            var estimatedTotalRecords = 0;
            foreach (var file in queue.Files)
            {
                // Estimate records by parsing file headers - this is a rough estimate
                try
                {
                    var dataTable = ParseFileToDataTable(file.FileData, file.FileExtension);
                    if (dataTable != null)
                    {
                        estimatedTotalRecords += dataTable.Rows.Count;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not estimate records for file {FileName}", file.FileName);
                    // Add a default estimate of 100 records per file if parsing fails
                    estimatedTotalRecords += 100;
                }
            }

            // Initial progress notification
            await _progressService.NotifyJobStarted(jobId, new JobStartDto
            {
                JobId = jobId,
                JobType = "FileQueue",
                TotalFiles = queue.Files.Count,
                EstimatedTotalRecords = estimatedTotalRecords,
                StartTime = DateTime.UtcNow
            });

            var totalFiles = queue.Files.Count;
            var completedFiles = 0;
            var failedFiles = 0;
            var totalProcessedRecords = 0;
            var totalFailedRecords = 0;

            foreach (var queuedFile in queue.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogInformation("Processing file {FileName} ({FileIndex}/{TotalFiles}) for job {JobId}", 
                    queuedFile.FileName, queuedFile.FileIndex + 1, totalFiles, jobId);

                queuedFile.Status = QueuedFileStatusEnum.Processing;
                queuedFile.StartedAt = DateTime.UtcNow;

                // Notify that this file is starting
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = (double)completedFiles / totalFiles * 100,
                    Status = "Processing",
                    CurrentOperation = $"Processing file: {queuedFile.FileName}",
                    ProcessedRecords = totalProcessedRecords,
                    TotalRecords = estimatedTotalRecords,
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
                        queuedFile.Status = QueuedFileStatusEnum.Completed;
                        queuedFile.CompletedAt = DateTime.UtcNow;
                        queuedFile.ProcessedRecords = result.Data.ProcessedRecords;
                        queuedFile.FailedRecords = result.Data.FailedRecords;
                        queuedFile.TotalRecords = result.Data.TotalRecords;
                        queuedFile.ProcessingTime = result.Data.ProcessingTime;
                        queuedFile.Errors = result.Data.Errors?.Select(e => e.ErrorMessage).ToList() ?? new List<string>();

                        // Update cumulative totals
                        totalProcessedRecords += result.Data.ProcessedRecords;
                        totalFailedRecords += result.Data.FailedRecords;
                        completedFiles++;

                        _logger.LogInformation("File {FileName} completed successfully. Processed: {ProcessedRecords}, Failed: {FailedRecords}", 
                            queuedFile.FileName, result.Data.ProcessedRecords, result.Data.FailedRecords);

                        // Send updated progress after file completion
                        await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                        {
                            JobId = jobId,
                            ProgressPercentage = (double)completedFiles / totalFiles * 100,
                            Status = completedFiles == totalFiles ? "Completed" : "Processing",
                            CurrentOperation = completedFiles == totalFiles ? "All files processed" : $"Completed file: {queuedFile.FileName}",
                            ProcessedRecords = totalProcessedRecords,
                            TotalRecords = estimatedTotalRecords,
                            CurrentFileName = queuedFile.FileName,
                            ProcessedFiles = completedFiles,
                            TotalFiles = totalFiles
                        });
                    }
                    else
                    {
                        queuedFile.Status = QueuedFileStatusEnum.Failed;
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
                    queuedFile.Status = QueuedFileStatusEnum.Cancelled;
                    throw; // Re-throw to handle at the queue level
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FileName} in job {JobId}", queuedFile.FileName, jobId);
                    
                    queuedFile.Status = QueuedFileStatusEnum.Failed;
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
            queue.Status = failedFiles == 0 ? QueueStatusEnum.Completed : QueueStatusEnum.CompletedWithErrors;
            queue.CompletedAt = DateTime.UtcNow;

            // Log the overall queue completion to BulkUploadHistory
            await LogQueueCompletionHistory(queue, completedFiles, failedFiles);

            // Create a summary response for the completed job
            var summaryResponse = new MultipleBulkUploadResponseDto
            {
                TotalFiles = totalFiles,
                ProcessedFiles = completedFiles,
                FailedFiles = failedFiles,
                TotalRecords = queue.Files.Sum(f => f.TotalRecords),
                ProcessedRecords = totalProcessedRecords,
                FailedRecords = totalFailedRecords,
                ProcessedAt = DateTime.UtcNow,
                TotalProcessingTime = DateTime.UtcNow - (queue.StartedAt ?? queue.CreatedAt),
                OverallSuccess = failedFiles == 0,
                FileResults = queue.Files.Select(f => new BulkUploadResponseDto
                {
                    FileName = f.FileName,
                    TotalRecords = f.TotalRecords,
                    ProcessedRecords = f.ProcessedRecords,
                    FailedRecords = f.FailedRecords,
                    ProcessingTime = f.ProcessingTime,
                    ProcessedAt = f.CompletedAt ?? DateTime.UtcNow,
                    Errors = f.Errors?.Select(e => new BulkUploadErrorDto { ErrorMessage = e }).ToList() ?? new List<BulkUploadErrorDto>()
                }).ToList()
            };

            await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
            {
                JobId = jobId,
                Success = failedFiles == 0,
                Message = failedFiles == 0 
                    ? $"All {totalFiles} files processed successfully. {totalProcessedRecords} total records processed." 
                    : $"Processing completed. {completedFiles} files succeeded, {failedFiles} files failed. {totalProcessedRecords} records processed.",
                Data = summaryResponse,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = DateTime.UtcNow - queue.CreatedAt
            });

            _logger.LogInformation("Queue processing completed for job {JobId}. Files: {TotalFiles}, Completed: {CompletedFiles}, Failed: {FailedFiles}", 
                jobId, totalFiles, completedFiles, failedFiles);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Queue processing was cancelled for job {JobId}", jobId);
            
            queue.Status = QueueStatusEnum.Cancelled;
            queue.CompletedAt = DateTime.UtcNow;

            // Mark any still-processing files as cancelled
            foreach (var file in queue.Files.Where(f => f.Status == QueuedFileStatusEnum.Processing || f.Status == QueuedFileStatusEnum.Queued))
            {
                file.Status = QueuedFileStatusEnum.Cancelled;
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
            
            queue.Status = QueueStatusEnum.Failed;
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

    public QueueStatusEnum GetQueueStatus(string jobId)
    {
        return _activeQueues.TryGetValue(jobId, out var queue) ? queue.Status : QueueStatusEnum.NotFound;
    }

    public List<QueuedFileInfoRequestDto> GetQueuedFiles(string jobId)
    {
        return _activeQueues.TryGetValue(jobId, out var queue) ? queue.Files : new List<QueuedFileInfoRequestDto>();
    }

    public bool CancelQueue(string jobId)
    {
        _logger.LogInformation("Attempting to cancel queue for job {JobId}", jobId);
        
        bool wasCancelled = _jobCancellationService.CancelJob(jobId);
        
        if (_activeQueues.TryGetValue(jobId, out var queue))
        {
            _logger.LogInformation("Found queue in active queues for job {JobId}, marking as cancelled", jobId);
            queue.Status = QueueStatusEnum.Cancelled;
            queue.CompletedAt = DateTime.UtcNow;
            
            foreach (var file in queue.Files.Where(f => f.Status == QueuedFileStatusEnum.Processing || f.Status == QueuedFileStatusEnum.Queued))
            {
                file.Status = QueuedFileStatusEnum.Cancelled;
                file.CompletedAt = DateTime.UtcNow;
            }
        }
        else
        {
            _logger.LogInformation("Queue not found in active queues for job {JobId}, but cancellation token was {Status}", 
                jobId, wasCancelled ? "found and cancelled" : "not found");
        }
        
        return wasCancelled;
    }

    private async Task LogQueueCompletionHistory(FileQueueRequestDto queue, int completedFiles, int failedFiles)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
            
            // Calculate totals from all files
            var totalRecords = queue.Files.Sum(f => f.TotalRecords);
            var processedRecords = queue.Files.Sum(f => f.ProcessedRecords);
            var failedRecords = queue.Files.Sum(f => f.FailedRecords);
            var processingTime = queue.CompletedAt.HasValue ? queue.CompletedAt.Value - queue.StartedAt.GetValueOrDefault() : TimeSpan.Zero;
            
            // Create a summary entry for the entire queue operation
            var history = new BulkUploadHistoryModel
            {
                UploadId = Guid.NewGuid(),
                UserId = queue.UserId,
                TableType = "Multiple Files", // Or could be the most common table type
                FileName = $"Queue Job ({completedFiles + failedFiles} files)",
                TotalRecords = totalRecords,
                ProcessedRecords = processedRecords,
                FailedRecords = failedRecords,
                UploadedAt = queue.StartedAt ?? queue.CreatedAt,
                Status = failedFiles == 0 ? "Success" : completedFiles == 0 ? "Failed" : "Partial",
                ProcessingTime = processingTime,
                ErrorDetails = queue.Files.Where(f => f.Errors != null && f.Errors.Any())
                    .SelectMany(f => f.Errors.Select(e => $"{f.FileName}: {e}"))
                    .Take(10) // Limit to first 10 errors
                    .Any() ? System.Text.Json.JsonSerializer.Serialize(
                    queue.Files.Where(f => f.Errors != null && f.Errors.Any())
                        .SelectMany(f => f.Errors.Select(e => $"{f.FileName}: {e}"))
                        .Take(10)
                        .ToList()
                ) : null
            };

            context.BulkUploadHistories.Add(history);
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Created queue completion BulkUploadHistory record for job {JobId} with {TotalFiles} files", 
                queue.JobId, completedFiles + failedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging queue completion history for job {JobId}", queue.JobId);
        }
    }

    private async Task<byte[]> ReadFileDataAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private DataTable? ParseFileToDataTable(byte[] fileData, string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".csv" => ParseCsvToDataTable(fileData),
            ".json" => ParseJsonToDataTable(fileData),
            ".xml" => ParseXmlToDataTable(fileData),
            ".xlsx" or ".xls" => ParseExcelToDataTable(fileData),
            _ => null
        };
    }

    private DataTable ParseCsvToDataTable(byte[] fileData)
    {
        var dataTable = new DataTable();
        
        using var memoryStream = new MemoryStream(fileData);
        using var reader = new StreamReader(memoryStream, Encoding.UTF8);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null
        });

        using var dr = new CsvDataReader(csv);
        dataTable.Load(dr);

        return dataTable;
    }

    private DataTable ParseJsonToDataTable(byte[] fileData)
    {
        var dataTable = new DataTable();

        try
        {
            var json = Encoding.UTF8.GetString(fileData);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

            if (jsonDoc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array || 
                jsonDoc.RootElement.GetArrayLength() == 0)
            {
                return dataTable;
            }

            // Get columns from first object
            var firstElement = jsonDoc.RootElement[0];
            if (firstElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                // Add columns based on first object properties
                foreach (var property in firstElement.EnumerateObject())
                {
                    dataTable.Columns.Add(property.Name);
                }

                // Add rows for all objects in array
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var row = dataTable.NewRow();
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            if (element.TryGetProperty(column.ColumnName, out var propertyValue))
                            {
                                row[column.ColumnName] = propertyValue.ValueKind switch
                                {
                                    System.Text.Json.JsonValueKind.String => propertyValue.GetString() ?? string.Empty,
                                    System.Text.Json.JsonValueKind.Number => propertyValue.GetRawText(),
                                    System.Text.Json.JsonValueKind.True => "true",
                                    System.Text.Json.JsonValueKind.False => "false",
                                    System.Text.Json.JsonValueKind.Null => string.Empty,
                                    _ => propertyValue.GetRawText()
                                };
                            }
                            else
                            {
                                row[column.ColumnName] = string.Empty;
                            }
                        }
                        dataTable.Rows.Add(row);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON to DataTable");
        }

        return dataTable;
    }

    private DataTable ParseXmlToDataTable(byte[] fileData)
    {
        var dataTable = new DataTable();

        try
        {
            var xml = Encoding.UTF8.GetString(fileData);
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            
            var root = doc.Root;
            if (root == null || !root.Elements().Any())
            {
                return dataTable;
            }

            // Get all unique element names from the first record to create columns
            var firstRecord = root.Elements().First();
            var columnNames = firstRecord.Elements()
                .Select(e => e.Name.LocalName)
                .Distinct()
                .ToList();

            // Add columns to DataTable
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            // Add rows for each record
            foreach (var record in root.Elements())
            {
                var row = dataTable.NewRow();
                foreach (DataColumn column in dataTable.Columns)
                {
                    var element = record.Elements()
                        .FirstOrDefault(e => e.Name.LocalName == column.ColumnName);
                    row[column.ColumnName] = element?.Value ?? string.Empty;
                }
                dataTable.Rows.Add(row);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing XML to DataTable");
        }

        return dataTable;
    }

    private DataTable ParseExcelToDataTable(byte[] fileData)
    {
        var dataTable = new DataTable();

        using var memoryStream = new MemoryStream(fileData);
        using var package = new ExcelPackage(memoryStream);
        
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null || worksheet.Dimension == null)
        {
            return dataTable;
        }

        // Add columns
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var columnName = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
            dataTable.Columns.Add(columnName);
        }

        // Add rows
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var dataRow = dataTable.NewRow();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
            }
            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }
}