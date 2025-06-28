using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Exceptions;
using Prototype.Services.BulkUpload;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/cancellation")]
public class BulkUploadCancellationController(
    ILogger<BulkUploadCancellationController> logger,
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    IJobCancellationService jobCancellationService,
    IProgressService progressService)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
    private readonly IJobCancellationService _jobCancellationService = jobCancellationService ?? throw new ArgumentNullException(nameof(jobCancellationService));
    private readonly IProgressService _progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));

    [HttpPost("cancel/{jobId}")]
    public async Task<IActionResult> CancelMigration(string jobId)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return HandleUserNotAuthenticated();
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

                return SuccessResponse(new { jobId, status = "cancelled" }, "Migration cancelled successfully");
            }
            else
            {
                return BadRequestWithMessage("Job not found or already completed");
            }
        }
        catch (DataNotFoundException ex)
        {
            Logger.LogWarning(ex, "Job not found for cancellation: {JobId}", jobId);
            return NotFound($"Job {jobId} not found");
        }
        catch (BusinessLogicException ex)
        {
            Logger.LogWarning(ex, "Business logic error cancelling job {JobId}", jobId);
            return BadRequestWithMessage(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            Logger.LogError(ex, "External service failure cancelling job {JobId}", jobId);
            return StatusCode(503, "Job cancellation service temporarily unavailable");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error cancelling migration for job {JobId}", jobId);
            return InternalServerError("An unexpected error occurred while cancelling the migration");
        }
    }
}