using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;
using Prototype.Services.BulkUpload;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/history")]
[ApiController]
public class BulkUploadHistoryController : ControllerBase
{
    private readonly IBulkUploadService _bulkUploadService;
    private readonly SentinelContext _context;
    private readonly ILogger<BulkUploadHistoryController> _logger;

    public BulkUploadHistoryController(
        IBulkUploadService bulkUploadService,
        SentinelContext context,
        ILogger<BulkUploadHistoryController> logger)
    {
        _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
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

            var history = await _bulkUploadService.GetUploadHistoryAsync(currentUser.UserId, page, pageSize);
            return Ok(new ApiResponseDto<PaginatedResult<BulkUploadHistory>>
            {
                Success = true,
                Message = "Upload history retrieved successfully",
                Data = history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload history");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while getting upload history",
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