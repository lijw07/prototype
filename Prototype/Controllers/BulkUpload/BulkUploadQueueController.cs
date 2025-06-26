using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.BulkUpload;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/queue")]
[ApiController]
public class BulkUploadQueueController : ControllerBase
{
    private readonly IFileQueueService _fileQueueService;
    private readonly SentinelContext _context;
    private readonly ILogger<BulkUploadQueueController> _logger;

    public BulkUploadQueueController(
        IFileQueueService fileQueueService,
        SentinelContext context,
        ILogger<BulkUploadQueueController> logger)
    {
        _fileQueueService = fileQueueService ?? throw new ArgumentNullException(nameof(fileQueueService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBulkDataWithQueue([FromForm] MultipleBulkUploadRequestDto requestDto)
    {
        try
        {
            // Temporary: Use admin user for testing
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
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

            _logger.LogInformation("Queueing {FileCount} files for processing", requestDto.Files.Count);

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
            _logger.LogError(ex, "Error queueing multiple files for bulk upload");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while queueing files for processing",
                Data = null
            });
        }
    }

    [HttpGet("status/{jobId}")]
    public IActionResult GetQueueStatus(string jobId)
    {
        try
        {
            var status = _fileQueueService.GetQueueStatus(jobId);
            var files = _fileQueueService.GetQueuedFiles(jobId);

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
            _logger.LogError(ex, "Error getting queue status for job {JobId}", jobId);
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while getting queue status",
                Data = null
            });
        }
    }

    [HttpPost("cancel/{jobId}")]
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

            _logger.LogInformation("Queue cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

            var wasCancelled = _fileQueueService.CancelQueue(jobId);
            
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
                    Message = "Failed to cancel file queue - job may not exist or already be completed",
                    Data = new { jobId }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling queue for job {JobId}", jobId);
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while cancelling the queue",
                Data = null
            });
        }
    }

    private async Task<UserModel?> GetCurrentUserAsync()
    {
        // Temporary: Return admin user for testing when authorization is disabled
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    }
}