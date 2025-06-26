using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Services.BulkUpload;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/history")]
public class BulkUploadHistoryController(
    ILogger<BulkUploadHistoryController> logger,
    SentinelContext context,
    IBulkUploadService bulkUploadService,
    IAuthenticatedUserAccessor userAccessor)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));

    [HttpGet]
    public async Task<IActionResult> GetUploadHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return HandleUserNotAuthenticated();

            var history = await _bulkUploadService.GetUploadHistoryAsync(currentUser.UserId, page, pageSize);
            return SuccessResponse(history, "Upload history retrieved successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting upload history");
            return InternalServerError("An error occurred while getting upload history");
        }
    }
}