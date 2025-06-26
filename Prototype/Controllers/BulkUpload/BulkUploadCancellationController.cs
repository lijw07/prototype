using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Models;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/cancellation")]
[ApiController]
public class BulkUploadCancellationController : BaseBulkUploadController
{
    private readonly IJobCancellationService _jobCancellationService;
    private readonly IProgressService _progressService;

    public BulkUploadCancellationController(
        SentinelContext context,
        ILogger<BulkUploadCancellationController> logger,
        IJobCancellationService jobCancellationService,
        IProgressService progressService)
        : base(context, logger)
    {
        _jobCancellationService = jobCancellationService ?? throw new ArgumentNullException(nameof(jobCancellationService));
        _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
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

            Logger.LogInformation("Cancellation requested for job {JobId} by user {UserId}", jobId, currentUser.UserId);

            // Cancel the job using the cancellation service
            var wasCancelled = _jobCancellationService.CancelJob(jobId);
            
            if (wasCancelled)
            {
                // Notify through SignalR that the job was canceled
                await _progressService.NotifyError(jobId, "Migration cancelled by user");
                
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
            Logger.LogError(ex, "Error cancelling migration for job {JobId}", jobId);
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "Failed to cancel migration",
                Data = null
            });
        }
    }
}