using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/progress")]
public class BulkUploadProgressController(
    ILogger<BulkUploadProgressController> logger,
    IAuthenticatedUserAccessor userAccessor,
    SentinelContext context,
    IBulkUploadService bulkUploadService,
    ITableDetectionService tableDetectionService,
    IProgressService progressService,
    IJobCancellationService jobCancellationService)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
    private readonly ITableDetectionService _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
    private readonly IProgressService _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
    private readonly IJobCancellationService _jobCancellationService = jobCancellationService ?? throw new ArgumentNullException(nameof(jobCancellationService));
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor;

    [HttpPost("upload-with-progress")]
    public async Task<IActionResult> UploadBulkDataWithProgress([FromForm] BulkUploadRequestDto requestDto)
    {
        Logger.LogInformation("BulkUploadProgressController.UploadBulkDataWithProgress called with file: {FileName}", requestDto?.File?.FileName ?? "null");
        
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return HandleUserNotAuthenticated();

            }

            if (requestDto.File == null || requestDto.File.Length == 0)
            {
                return BadRequestWithMessage("No file provided");
            }

            // Use provided job ID or generated a new one for progress tracking
            var jobId = !string.IsNullOrEmpty(requestDto.JobId) ? requestDto.JobId : _progressService.GenerateJobId();

            // Read file data immediately
            var fileData = await ReadFileDataAsync(requestDto.File);
            if (!ValidateFileExtension(requestDto.File.FileName, out var fileExtension))
            {
                return BadRequestWithMessage("Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS");
            }
            
            // Detect a table type immediately
            var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
            if (detectedTable == null)
            {
                return BadRequestWithMessage("Could not determine table type from file data");
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
                
                return BadRequestWithMessage(uploadResult.ErrorMessage);
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

            var response = ApiResponse<object>.Success(new { JobId = jobId, Result = uploadResult.Data }, "Bulk upload completed successfully");
            
                return SuccessResponse(response);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Bulk upload job {JobId} was cancelled", jobId);
                return SuccessResponse(
                    new { JobId = jobId }, "Migration was cancelled");
            }
            finally
            {
                _jobCancellationService.RemoveJob(jobId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in bulk upload with progress");
            return StatusCode(500, ApiResponse<object>.InternalServerError("An error occurred during bulk upload"));
        }
    }
}