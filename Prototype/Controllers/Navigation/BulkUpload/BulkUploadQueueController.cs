using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Services.BulkUpload;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/queue")]
public class BulkUploadQueueController(
    ILogger<BulkUploadQueueController> logger,
    SentinelContext context,
    IFileQueueService fileQueueService,
    IAuthenticatedUserAccessor userAccessor)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IFileQueueService _fileQueueService = fileQueueService ?? throw new ArgumentNullException(nameof(fileQueueService));
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBulkDataWithQueue([FromForm] MultipleBulkUploadRequestDto requestDto)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            if (requestDto.Files.IsNullOrEmpty())
                return BadRequestWithMessage("No files uploaded");

            // Validate all files first
            foreach (var file in requestDto.Files)
            {
                if (!ValidateFileExtension(file.FileName, out _))
                {
                    return BadRequestWithMessage($"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS");
                }
            }

            Logger.LogInformation("Queueing {FileCount} files for processing", requestDto.Files.Count);

            // Create queue request
            var queueRequest = new QueuedFileUploadRequestDto
            {
                Files = requestDto.Files,
                UserId = currentUser.UserId,
                IgnoreErrors = requestDto.IgnoreErrors,
                ContinueOnError = requestDto.ContinueOnError
            };

            // Queue the files for processing
            var jobId = await _fileQueueService.QueueMultipleFilesAsync(queueRequest);

            return SuccessResponse(new 
                { 
                    JobId = jobId,
                    TotalFiles = requestDto.Files.Count,
                    Status = "Queued",
                    Message = "Files are queued and will be processed in order"
                }, $"Files queued for processing. {requestDto.Files.Count} files will be processed sequentially.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error queueing multiple files for bulk upload");
            return InternalServerError("An error occurred while queueing files for processing");
        }
    }

    [HttpGet("status/{jobId}")]
    public IActionResult GetQueueStatus(string jobId)
    {
        try
        {
            var status = _fileQueueService.GetQueueStatus(jobId);
            var files = _fileQueueService.GetQueuedFiles(jobId);

            return SuccessResponse(new
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
                }, "Queue status retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting queue status for job {JobId}", jobId);
            return InternalServerError("An error occurred while getting queue status");
        }
    }

    [HttpPost("cancel/{jobId}")]
    public async Task<IActionResult> CancelQueue(string jobId)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            Logger.LogInformation("Queue cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

            var wasCancelled = _fileQueueService.CancelQueue(jobId);
            
            if (wasCancelled)
            {
                return SuccessResponse(new { jobId, status = "cancelled" }, "File queue cancelled successfully");
            }
            else
            {
                return BadRequestWithMessage("Failed to cancel file queue - job may not exist or already be completed");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling queue for job {JobId}", jobId);
            return InternalServerError("An error occurred while cancelling the queue");
        }
    }
}