using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.Models;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using Prototype.Helpers;

namespace Prototype.Controllers.BulkUpload
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BulkUploadController(
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
        : ControllerBase
    {
        private readonly IValidationService _validationService = validationService;
        private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor;

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBulkData([FromForm] BulkUploadRequestDto requestDto)
        {
            logger.LogInformation("BulkUploadController.UploadBulkData called with file: {FileName}", requestDto?.File?.FileName ?? "null");
            
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (requestDto.File == null || requestDto.File.Length == 0)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "No file uploaded",
                        Data = null
                    });
                }

                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(requestDto.File.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS",
                        Data = null
                    });
                }

                var uploadResult = await transactionService.ExecuteInTransactionAsync(async () =>
                {
                    var fileData = await ReadFileDataAsync(requestDto.File);
                    
                    var detectedTable = await tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                    if (detectedTable == null)
                    {
                        return Result<BulkUploadResponseDto>.Failure("Could not determine table type from file data");
                    }

                    var validationResult = await bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
                    if (!validationResult.IsSuccess)
                    {
                        return Result<BulkUploadResponseDto>.Failure($"Validation failed: {validationResult.ErrorMessage}");
                    }

                    var processResult = await bulkUploadService.ProcessBulkDataAsync(
                        fileData, 
                        detectedTable.TableType, 
                        fileExtension,
                        currentUser.UserId,
                        requestDto.IgnoreErrors);

                    await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                        processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

                    return processResult;
                });

                if (!uploadResult.IsSuccess)
                {
                    var errorResponse = new ApiResponseDto<BulkUploadResponseDto>
                    {
                        Success = false,
                        Message = uploadResult.ErrorMessage,
                        Data = null
                    };
                    
                    logger.LogWarning("Returning error bulk upload response: {Response}", 
                        System.Text.Json.JsonSerializer.Serialize(errorResponse));
                    
                    return BadRequest(errorResponse);
                }

                // Add file context to response
                if (uploadResult.Data != null)
                {
                    uploadResult.Data.FileName = requestDto.File.FileName;
                    uploadResult.Data.FileIndex = requestDto.FileIndex;
                    uploadResult.Data.TotalFiles = requestDto.TotalFiles;
                }

                var response = new ApiResponseDto<BulkUploadResponseDto>
                {
                    Success = true,
                    Message = "Bulk upload completed successfully",
                    Data = uploadResult.Data
                };
                
                logger.LogInformation("Returning successful bulk upload response: {Response}", 
                    System.Text.Json.JsonSerializer.Serialize(response));
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in bulk upload");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-with-progress")]
        public async Task<IActionResult> UploadBulkDataWithProgress([FromForm] BulkUploadRequestDto requestDto)
        {
            logger.LogInformation("BulkUploadController.UploadBulkDataWithProgress called with file: {FileName}", requestDto?.File?.FileName ?? "null");
            
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (requestDto.File == null || requestDto.File.Length == 0)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "No file provided",
                        Data = null
                    });
                }

                // Use provided job ID or generated a new one for progress tracking
                var jobId = !string.IsNullOrEmpty(requestDto.JobId) ? requestDto.JobId : progressService.GenerateJobId();

                // Read file data immediately
                var fileData = await ReadFileDataAsync(requestDto.File);
                var fileExtension = Path.GetExtension(requestDto.File.FileName).ToLowerInvariant();
                
                // Detect a table type immediately
                var detectedTable = await tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                if (detectedTable == null)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Could not determine table type from file data",
                        Data = null
                    });
                }

                // Create a cancellation token for this job
                var cancellationTokenSource = jobCancellationService.CreateJobCancellation(jobId);
                
                try
                {
                    // Generate job ID for progress tracking and return with a result
                    var uploadResult = await bulkUploadService.ProcessBulkDataWithProgressAsync(
                        fileData, 
                        detectedTable.TableType, 
                        fileExtension, 
                        currentUser.UserId, 
                        jobId,
                        requestDto.File.FileName,
                        0, // fileIndex
                        1, // totalFiles
                        requestDto.IgnoreErrors == true,
                        cancellationTokenSource.Token
                    );

                if (!uploadResult.IsSuccess)
                {
                    logger.LogWarning("Bulk upload failed: {Error}", uploadResult.ErrorMessage);
                    
                    return BadRequest(new ApiResponseDto<object>
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
                    uploadResult.Data.FileName = requestDto.File.FileName;
                    uploadResult.Data.FileIndex = 0;
                    uploadResult.Data.TotalFiles = 1;
                }

                var response = new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Bulk upload completed successfully",
                    Data = new { JobId = jobId, Result = uploadResult.Data }
                };
                
                    return Ok(response);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Bulk upload job {JobId} was cancelled", jobId);
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Migration was cancelled",
                        Data = new { JobId = jobId }
                    });
                }
                finally
                {
                    // Clean up the cancellation token
                    jobCancellationService.RemoveJob(jobId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in bulk upload with progress");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleBulkData([FromForm] MultipleBulkUploadRequestDto requestDto)
        {
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (requestDto.Files.IsNullOrEmpty())
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "No files uploaded",
                        Data = null
                    });
                }

                // Validate all files first
                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                foreach (var file in requestDto.Files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new ApiResponseDto<object>
                        {
                            Success = false,
                            Message = $"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS",
                            Data = null
                        });
                    }
                }

                var response = new MultipleBulkUploadResponseDto
                {
                    TotalFiles = requestDto.Files.Count,
                    ProcessedAt = DateTime.UtcNow
                };

                var overallStartTime = DateTime.UtcNow;
                var processedFiles = 0;
                var failedFiles = 0;

                // Process files sequentially or in parallel based on request
                if (requestDto.ProcessFilesSequentially)
                {
                    // Sequential processing
                    for (int i = 0; i < requestDto.Files.Count; i++)
                    {
                        var file = requestDto.Files[i];
                        try
                        {
                            var fileResult = await ProcessSingleFileAsync(file, currentUser, requestDto.IgnoreErrors, i, requestDto.Files.Count);
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
                            logger.LogError(ex, "Error processing file {FileName}", file.FileName);
                            
                            response.GlobalErrors.Add($"Failed to process {file.FileName}: {ex.Message}");
                            
                            if (!requestDto.ContinueOnError)
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
                    return BadRequest(new ApiResponseDto<object>
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

                return Ok(new ApiResponseDto<MultipleBulkUploadResponseDto>
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
                logger.LogError(ex, "Error in multiple file bulk upload");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred during multiple file bulk upload",
                    Data = null
                });
            }
        }

        [HttpPost("upload-queue")]
        public async Task<IActionResult> UploadBulkDataWithQueue([FromForm] MultipleBulkUploadRequestDto requestDto)
        {
            try
            {
                // Temporary: Use admin user for testing
                var currentUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Admin user not found",
                        Data = null
                    });
                }

                if (requestDto.Files.IsNullOrEmpty())
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "No files uploaded",
                        Data = null
                    });
                }

                // Validate all files first
                var allowedExtensions = new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
                foreach (var file in requestDto.Files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new ApiResponseDto<object>
                        {
                            Success = false,
                            Message = $"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS",
                            Data = null
                        });
                    }
                }

                logger.LogInformation("Queueing {FileCount} files for processing", requestDto.Files.Count);

                // Create queue request
                var queueRequest = new QueuedFileUploadRequestDto
                {
                    Files = requestDto.Files,
                    UserId = currentUser.UserId,
                    IgnoreErrors = requestDto.IgnoreErrors,
                    ContinueOnError = requestDto.ContinueOnError
                };

                // Queue the files for processing
                var jobId = await fileQueueService.QueueMultipleFilesAsync(queueRequest);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Files queued for processing. {requestDto.Files.Count} files will be processed sequentially.",
                    Data = new 
                    { 
                        JobId = jobId,
                        TotalFiles = requestDto.Files.Count,
                        Status = "Queued",
                        Message = "Files are queued and will be processed in order"
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error queueing multiple files for bulk upload");
                return StatusCode(500, new ApiResponseDto<object>
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
                var status = fileQueueService.GetQueueStatus(jobId);
                var files = fileQueueService.GetQueuedFiles(jobId);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Queue status retrieved successfully",
                    Data = new
                    {
                        JobId = jobId,
                        Status = status.ToString(),
                        TotalFiles = files.Count,
                        CompletedFiles = files.Count(f => f.Status == QueuedFileStatusEnum.Completed),
                        FailedFiles = files.Count(f => f.Status == QueuedFileStatusEnum.Failed),
                        ProcessingFile = files.FirstOrDefault(f => f.Status == QueuedFileStatusEnum.Processing)?.FileName,
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
                logger.LogError(ex, "Error getting queue status for job {JobId}", jobId);
                return StatusCode(500, new ApiResponseDto<object>
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
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                logger.LogInformation("Queue cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

                var wasCancelled = fileQueueService.CancelQueue(jobId);
                
                if (wasCancelled)
                {
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "File queue cancelled successfully",
                        Data = new { jobId, status = "cancelled" }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Queue not found or already completed",
                        Data = new { jobId }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling queue for job {JobId}", jobId);
                return StatusCode(500, new ApiResponseDto<object>
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
                var template = await bulkUploadService.GetTemplateAsync(tableType);
                if (template == null)
                {
                    return NotFound(new ApiResponseDto<object>
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
                logger.LogError(ex, "Error getting upload template for {TableType}", tableType);
                return StatusCode(500, new ApiResponseDto<object>
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
                var tables = await tableDetectionService.GetSupportedTablesAsync();
                return Ok(new ApiResponseDto<List<SupportedTableInfoDto>>
                {
                    Success = true,
                    Message = "Supported tables retrieved successfully",
                    Data = tables
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting supported tables");
                return StatusCode(500, new ApiResponseDto<object>
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
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                var history = await bulkUploadService.GetUploadHistoryAsync(currentUser.UserId, page, pageSize);
                return Ok(new ApiResponseDto<PaginatedResult<BulkUploadHistory>>
                {
                    Success = true,
                    Message = "Upload history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting upload history");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "An error occurred while getting upload history",
                    Data = null
                });
            }
        }


        private async Task<BulkUploadResponseDto> ProcessSingleFileAsync(IFormFile file, UserModel currentUser, bool ignoreErrors, int fileIndex, int totalFiles)
        {
            var fileData = await ReadFileDataAsync(file);
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            var detectedTable = await tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
            if (detectedTable == null)
            {
                throw new InvalidOperationException($"Could not determine table type from file data for {file.FileName}");
            }

            var validationResult = await bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
            if (!validationResult.IsSuccess)
            {
                throw new InvalidOperationException($"Validation failed for {file.FileName}: {validationResult.ErrorMessage}");
            }

            var processResult = await bulkUploadService.ProcessBulkDataAsync(
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

            return processResult.Data ?? new BulkUploadResponseDto();
        }

        private async Task LogMultipleBulkUploadActivity(Guid userId, MultipleBulkUploadResponseDto responseDto)
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                DeviceInformation = HttpContext.Request.Headers["User-Agent"].ToString(),
                ActionType = Enum.ActionTypeEnum.Create,
                Description = $"Multiple file bulk upload. Files: {responseDto.TotalFiles}, Processed: {responseDto.ProcessedFiles}, Failed: {responseDto.FailedFiles}, Total Records: {responseDto.TotalRecords}, Success: {responseDto.OverallSuccess}",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            context.UserActivityLogs.Add(activityLog);
            await context.SaveChangesAsync();
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
                    return Unauthorized(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                logger.LogInformation("Cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

                // Cancel the job using the cancellation service
                var wasCancelled = jobCancellationService.CancelJob(jobId);
                
                if (wasCancelled)
                {
                    // Notify through SignalR that the job was canceled
                    await progressService.NotifyError(jobId, "Migration cancelled by user");
                    
                    // Log the cancellation activity
                    await LogBulkUploadActivity(currentUser.UserId, "Migration", 0, false);

                    return Ok(new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "Migration cancelled successfully",
                        Data = new { jobId, status = "cancelled" }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Job not found or already completed",
                        Data = new { jobId }
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cancelling migration for job {JobId}", jobId);
                return StatusCode(500, new ApiResponseDto<object>
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
            return await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
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

            context.UserActivityLogs.Add(activityLog);
            await context.SaveChangesAsync();
        }
    }
}