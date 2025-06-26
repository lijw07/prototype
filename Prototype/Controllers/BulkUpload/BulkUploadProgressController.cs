using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/progress")]
[ApiController]
public class BulkUploadProgressController : BaseBulkUploadController
{
    private readonly IBulkUploadService _bulkUploadService;
    private readonly ITableDetectionService _tableDetectionService;
    private readonly IProgressService _progressService;
    private readonly IJobCancellationService _jobCancellationService;

    public BulkUploadProgressController(
        IBulkUploadService bulkUploadService,
        ITableDetectionService tableDetectionService,
        SentinelContext context,
        ILogger<BulkUploadProgressController> logger,
        IProgressService progressService,
        IJobCancellationService jobCancellationService)
        : base(context, logger)
    {
        _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
        _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
        _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
        _jobCancellationService = jobCancellationService ?? throw new ArgumentNullException(nameof(jobCancellationService));
    }

    [HttpPost("upload-with-progress")]
    public async Task<IActionResult> UploadBulkDataWithProgress([FromForm] BulkUploadRequestDto requestDto)
    {
        Logger.LogInformation("BulkUploadProgressController.UploadBulkDataWithProgress called with file: {FileName}", requestDto?.File?.FileName ?? "null");
        
        try
        {
            var currentUser = await GetCurrentUserAsync();
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
            var jobId = !string.IsNullOrEmpty(requestDto.JobId) ? requestDto.JobId : _progressService.GenerateJobId();

            // Read file data immediately
            var fileData = await ReadFileDataAsync(requestDto.File);
            if (!ValidateFileExtension(requestDto.File.FileName, out var fileExtension))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS",
                    Data = null
                });
            }
            
            // Detect a table type immediately
            var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
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
                    requestDto.File.FileName,
                    0, // fileIndex
                    1, // totalFiles
                    requestDto.IgnoreErrors == true,
                    cancellationTokenSource.Token
                );

            if (!uploadResult.IsSuccess)
            {
                Logger.LogWarning("Bulk upload failed: {Error}", uploadResult.ErrorMessage);
                
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
                Logger.LogInformation("Bulk upload job {JobId} was cancelled", jobId);
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
                _jobCancellationService.RemoveJob(jobId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in bulk upload with progress");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during bulk upload",
                Data = null
            });
        }
    }
}