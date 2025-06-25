using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.Models;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using Prototype.Helpers;

namespace Prototype.Controllers.BulkUpload
{
    // [Authorize] // Temporarily disabled for testing
    [Route("api/[controller]")]
    [ApiController]
    public class BulkUploadController : ControllerBase
    {
        private readonly IBulkUploadService _bulkUploadService;
        private readonly ITableDetectionService _tableDetectionService;
        private readonly SentinelContext _context;
        private readonly ILogger<BulkUploadController> _logger;
        private readonly IValidationService _validationService;
        private readonly ITransactionService _transactionService;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IProgressService _progressService;
        private readonly IJobCancellationService _jobCancellationService;
        private readonly IFileQueueService _fileQueueService;

        public BulkUploadController(
            IBulkUploadService bulkUploadService,
            ITableDetectionService tableDetectionService,
            SentinelContext context,
            ILogger<BulkUploadController> logger,
            IValidationService validationService,
            ITransactionService transactionService,
            IAuthenticatedUserAccessor userAccessor,
            IProgressService progressService,
            IJobCancellationService jobCancellationService,
            IFileQueueService fileQueueService)
        {
            _bulkUploadService = bulkUploadService;
            _tableDetectionService = tableDetectionService;
            _context = context;
            _logger = logger;
            _validationService = validationService;
            _transactionService = transactionService;
            _userAccessor = userAccessor;
            _progressService = progressService;
            _jobCancellationService = jobCancellationService;
            _fileQueueService = fileQueueService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBulkData([FromForm] BulkUploadRequest request)
        {
            _logger.LogInformation("BulkUploadController.UploadBulkData called with file: {FileName}", request?.File?.FileName ?? "null");
            
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No file uploaded",
                        Data = null
                    });
                }

                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS",
                        Data = null
                    });
                }

                var uploadResult = await _transactionService.ExecuteInTransactionAsync(async () =>
                {
                    var fileData = await ReadFileDataAsync(request.File);
                    
                    var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                    if (detectedTable == null)
                    {
                        return Result<BulkUploadResponse>.Failure("Could not determine table type from file data");
                    }

                    var validationResult = await _bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
                    if (!validationResult.IsSuccess)
                    {
                        return Result<BulkUploadResponse>.Failure($"Validation failed: {validationResult.ErrorMessage}");
                    }

                    var processResult = await _bulkUploadService.ProcessBulkDataAsync(
                        fileData, 
                        detectedTable.TableType, 
                        fileExtension,
                        currentUser.UserId,
                        request.IgnoreErrors);

                    await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                        processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

                    return processResult;
                });

                if (!uploadResult.IsSuccess)
                {
                    var errorResponse = new ApiResponse<BulkUploadResponse>
                    {
                        Success = false,
                        Message = uploadResult.ErrorMessage,
                        Data = null
                    };
                    
                    _logger.LogWarning("Returning error bulk upload response: {Response}", 
                        System.Text.Json.JsonSerializer.Serialize(errorResponse));
                    
                    return BadRequest(errorResponse);
                }

                // Add file context to response
                if (uploadResult.Data != null)
                {
                    uploadResult.Data.FileName = request.File.FileName;
                    uploadResult.Data.FileIndex = request.FileIndex;
                    uploadResult.Data.TotalFiles = request.TotalFiles;
                }

                var response = new ApiResponse<BulkUploadResponse>
                {
                    Success = true,
                    Message = "Bulk upload completed successfully",
                    Data = uploadResult.Data
                };
                
                _logger.LogInformation("Returning successful bulk upload response: {Response}", 
                    System.Text.Json.JsonSerializer.Serialize(response));
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk upload");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-with-progress")]
        public async Task<IActionResult> UploadBulkDataWithProgress([FromForm] BulkUploadRequest request)
        {
            _logger.LogInformation("BulkUploadController.UploadBulkDataWithProgress called with file: {FileName}", request?.File?.FileName ?? "null");
            
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No file provided",
                        Data = null
                    });
                }

                // Use provided job ID or generated a new one for progress tracking
                var jobId = !string.IsNullOrEmpty(request.JobId) ? request.JobId : _progressService.GenerateJobId();

                // Read file data immediately
                var fileData = await ReadFileDataAsync(request.File);
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                
                // Detect a table type immediately
                var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                if (detectedTable == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Could not determine table type from file data",
                        Data = null
                    });
                }

                // Create a cancellation token for this job
                var cancellationTokenSource = _jobCancellationService.CreateJobCancellation(jobId);
                
                try
                {
                    // Generate job ID for progress tracking and return with a result
                    var uploadResult = await _bulkUploadService.ProcessBulkDataWithProgressAsync(
                        fileData, 
                        detectedTable.TableType, 
                        fileExtension, 
                        currentUser.UserId, 
                        jobId,
                        request.File.FileName,
                        0, // fileIndex
                        1, // totalFiles
                        request.IgnoreErrors == true,
                        cancellationTokenSource.Token
                    );

                if (!uploadResult.IsSuccess)
                {
                    _logger.LogWarning("Bulk upload failed: {Error}", uploadResult.ErrorMessage);
                    
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = uploadResult.ErrorMessage,
                        Data = new { JobId = jobId, Result = uploadResult.Data }
                    });
                }

                await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                    uploadResult.Data!.ProcessedRecords, uploadResult.IsSuccess);

                // Add file context to response
                if (uploadResult.Data != null)
                {
                    uploadResult.Data.FileName = request.File.FileName;
                    uploadResult.Data.FileIndex = 0;
                    uploadResult.Data.TotalFiles = 1;
                }

                var response = new ApiResponse<object>
                {
                    Success = true,
                    Message = "Bulk upload completed successfully",
                    Data = new { JobId = jobId, Result = uploadResult.Data }
                };
                
                    return Ok(response);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Bulk upload job {JobId} was cancelled", jobId);
                    return Ok(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Migration was cancelled",
                        Data = new { JobId = jobId }
                    });
                }
                finally
                {
                    // Clean up the cancellation token
                    _jobCancellationService.RemoveJob(jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk upload with progress");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleBulkData([FromForm] MultipleBulkUploadRequest request)
        {
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (request.Files.IsNullOrEmpty())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No files uploaded",
                        Data = null
                    });
                }

                // Validate all files first
                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                foreach (var file in request.Files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS",
                            Data = null
                        });
                    }
                }

                var response = new MultipleBulkUploadResponse
                {
                    TotalFiles = request.Files.Count,
                    ProcessedAt = DateTime.UtcNow
                };

                var overallStartTime = DateTime.UtcNow;
                var processedFiles = 0;
                var failedFiles = 0;

                // Process files sequentially or in parallel based on request
                if (request.ProcessFilesSequentially)
                {
                    // Sequential processing
                    for (int i = 0; i < request.Files.Count; i++)
                    {
                        var file = request.Files[i];
                        try
                        {
                            var fileResult = await ProcessSingleFileAsync(file, currentUser, request.IgnoreErrors, i, request.Files.Count);
                            response.FileResults.Add(fileResult);
                            
                            if (fileResult.ProcessedRecords > 0)
                            {
                                processedFiles++;
                                response.ProcessedRecords += fileResult.ProcessedRecords;
                            }
                            
                            if (fileResult.FailedRecords > 0)
                            {
                                response.FailedRecords += fileResult.FailedRecords;
                            }
                            
                            response.TotalRecords += fileResult.TotalRecords;
                        }
                        catch (Exception ex)
                        {
                            failedFiles++;
                            _logger.LogError(ex, "Error processing file {FileName}", file.FileName);
                            
                            response.GlobalErrors.Add($"Failed to process {file.FileName}: {ex.Message}");
                            
                            if (!request.ContinueOnError)
                            {
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Parallel processing (if needed in future)
                    // This can be implemented later for better performance
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Parallel processing is not currently supported. Please use sequential processing.",
                        Data = null
                    });
                }

                response.ProcessedFiles = processedFiles;
                response.FailedFiles = failedFiles;
                response.TotalProcessingTime = DateTime.UtcNow - overallStartTime;
                response.OverallSuccess = failedFiles == 0 && response.GlobalErrors.Count == 0;

                // Log the overall activity
                await LogMultipleBulkUploadActivity(currentUser.UserId, response);

                return Ok(new ApiResponse<MultipleBulkUploadResponse>
                {
                    Success = response.OverallSuccess,
                    Message = response.OverallSuccess 
                        ? "Multiple file bulk upload completed successfully" 
                        : "Multiple file bulk upload completed with some errors",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in multiple file bulk upload");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during multiple file bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-queue")]
        public async Task<IActionResult> UploadBulkDataWithQueue([FromForm] MultipleBulkUploadRequest request)
        {
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (request.Files.IsNullOrEmpty())
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No files uploaded",
                        Data = null
                    });
                }

                // Validate all files first
                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                foreach (var file in request.Files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = $"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS",
                            Data = null
                        });
                    }
                }

                _logger.LogInformation("Queueing {FileCount} files for processing", request.Files.Count);

                // Create queue request
                var queueRequest = new QueuedFileUploadRequest
                {
                    Files = request.Files,
                    UserId = currentUser.UserId,
                    IgnoreErrors = request.IgnoreErrors,
                    ContinueOnError = request.ContinueOnError
                };

                // Queue the files for processing
                var jobId = await _fileQueueService.QueueMultipleFilesAsync(queueRequest);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Files queued for processing. {request.Files.Count} files will be processed sequentially.",
                    Data = new 
                    { 
                        JobId = jobId,
                        TotalFiles = request.Files.Count,
                        Status = "Queued",
                        Message = "Files are queued and will be processed in order"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing multiple files for bulk upload");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while queueing files for processing",
                    Data = null
                });
            }
        }

        [HttpGet("queue-status/{jobId}")]
        public IActionResult GetQueueStatus(string jobId)
        {
            try
            {
                var status = _fileQueueService.GetQueueStatus(jobId);
                var files = _fileQueueService.GetQueuedFiles(jobId);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Queue status retrieved successfully",
                    Data = new
                    {
                        JobId = jobId,
                        Status = status.ToString(),
                        TotalFiles = files.Count,
                        CompletedFiles = files.Count(f => f.Status == QueuedFileStatus.Completed),
                        FailedFiles = files.Count(f => f.Status == QueuedFileStatus.Failed),
                        ProcessingFile = files.FirstOrDefault(f => f.Status == QueuedFileStatus.Processing)?.FileName,
                        Files = files.Select(f => new
                        {
                            f.FileName,
                            Status = f.Status.ToString(),
                            f.ProcessedRecords,
                            f.FailedRecords,
                            f.TotalRecords,
                            ProcessingTimeMs = f.ProcessingTime.TotalMilliseconds,
                            ErrorCount = f.Errors?.Count ?? 0,
                            f.QueuedAt,
                            f.StartedAt,
                            f.CompletedAt
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status for job {JobId}", jobId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting queue status",
                    Data = null
                });
            }
        }

        [HttpPost("cancel-queue/{jobId}")]
        public async Task<IActionResult> CancelQueue(string jobId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                _logger.LogInformation("Queue cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

                var wasCancelled = _fileQueueService.CancelQueue(jobId);
                
                if (wasCancelled)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "File queue cancelled successfully",
                        Data = new { jobId, status = "cancelled" }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Queue not found or already completed",
                        Data = new { jobId }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling queue for job {JobId}", jobId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to cancel queue",
                    Data = null
                });
            }
        }

        [HttpGet("templates/{tableType}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUploadTemplate(string tableType)
        {
            try
            {
                var template = await _bulkUploadService.GetTemplateAsync(tableType);
                if (template == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = $"Template not found for table type: {tableType}",
                        Data = null
                    });
                }

                return File(template.Content, template.ContentType, template.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload template for {TableType}", tableType);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting the template",
                    Data = null
                });
            }
        }

        [HttpGet("supported-tables")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSupportedTables()
        {
            try
            {
                var tables = await _tableDetectionService.GetSupportedTablesAsync();
                return Ok(new ApiResponse<List<SupportedTableInfo>>
                {
                    Success = true,
                    Message = "Supported tables retrieved successfully",
                    Data = tables
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported tables");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting supported tables",
                    Data = null
                });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetUploadHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                var history = await _bulkUploadService.GetUploadHistoryAsync(currentUser.UserId, page, pageSize);
                return Ok(new ApiResponse<PaginatedResult<BulkUploadHistory>>
                {
                    Success = true,
                    Message = "Upload history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload history");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting upload history",
                    Data = null
                });
            }
        }


        private async Task<BulkUploadResponse> ProcessSingleFileAsync(IFormFile file, UserModel currentUser, bool ignoreErrors, int fileIndex, int totalFiles)
        {
            var fileData = await ReadFileDataAsync(file);
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
            if (detectedTable == null)
            {
                throw new InvalidOperationException($"Could not determine table type from file data for {file.FileName}");
            }

            var validationResult = await _bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
            if (!validationResult.IsSuccess)
            {
                throw new InvalidOperationException($"Validation failed for {file.FileName}: {validationResult.ErrorMessage}");
            }

            var processResult = await _bulkUploadService.ProcessBulkDataAsync(
                fileData, 
                detectedTable.TableType, 
                fileExtension,
                currentUser.UserId,
                ignoreErrors);

            if (!processResult.IsSuccess)
            {
                throw new InvalidOperationException($"Processing failed for {file.FileName}: {processResult.ErrorMessage}");
            }

            // Add file context to the result
            if (processResult.Data != null)
            {
                processResult.Data.FileName = file.FileName;
                processResult.Data.FileIndex = fileIndex;
                processResult.Data.TotalFiles = totalFiles;
                
                // Add file name to all errors
                foreach (var error in processResult.Data.Errors)
                {
                    error.FileName = file.FileName;
                }
            }

            await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

            return processResult.Data ?? new BulkUploadResponse();
        }

        private async Task LogMultipleBulkUploadActivity(Guid userId, MultipleBulkUploadResponse response)
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                DeviceInformation = HttpContext.Request.Headers["User-Agent"].ToString(),
                ActionType = Enum.ActionTypeEnum.Create,
                Description = $"Multiple file bulk upload. Files: {response.TotalFiles}, Processed: {response.ProcessedFiles}, Failed: {response.FailedFiles}, Total Records: {response.TotalRecords}, Success: {response.OverallSuccess}",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }

        private async Task<byte[]> ReadFileDataAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        [HttpPost("cancel/{jobId}")]
        public async Task<IActionResult> CancelMigration(string jobId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                _logger.LogInformation("Cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

                // Cancel the job using the cancellation service
                var wasCancelled = _jobCancellationService.CancelJob(jobId);
                
                if (wasCancelled)
                {
                    // Notify through SignalR that the job was canceled
                    await _progressService.NotifyError(jobId, "Migration cancelled by user");
                    
                    // Log the cancellation activity
                    await LogBulkUploadActivity(currentUser.UserId, "Migration", 0, false);

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Migration cancelled successfully",
                        Data = new { jobId, status = "cancelled" }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Job not found or already completed",
                        Data = new { jobId }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling migration for job {JobId}", jobId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to cancel migration",
                    Data = null
                });
            }
        }

        private async Task<UserModel?> GetCurrentUserAsync()
        {
            // Temporary: Return admin user for testing when authorization is disabled
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        }

        private async Task LogBulkUploadActivity(Guid userId, string tableType, int recordCount, bool success)
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                DeviceInformation = HttpContext.Request.Headers["User-Agent"].ToString(),
                ActionType = Enum.ActionTypeEnum.Create,
                Description = $"Bulk upload to {tableType} table. Records: {recordCount}. Success: {success}",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }
}