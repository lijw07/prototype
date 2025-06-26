using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/templates")]
[ApiController]
public class BulkUploadTemplateController : ControllerBase
{
    private readonly IBulkUploadService _bulkUploadService;
    private readonly ITableDetectionService _tableDetectionService;
    private readonly ILogger<BulkUploadTemplateController> _logger;

    public BulkUploadTemplateController(
        IBulkUploadService bulkUploadService,
        ITableDetectionService tableDetectionService,
        ILogger<BulkUploadTemplateController> logger)
    {
        _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
        _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("{tableType}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUploadTemplate(string tableType)
    {
        try
        {
            var template = await _bulkUploadService.GetTemplateAsync(tableType);
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
            _logger.LogError(ex, "Error getting upload template for {TableType}", tableType);
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while getting the template",
                Data = null
            });
        }
    }

    [HttpGet("supported")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSupportedTables()
    {
        try
        {
            var tables = await _tableDetectionService.GetSupportedTablesAsync();
            return Ok(new ApiResponseDto<List<SupportedTableInfoDto>>
            {
                Success = true,
                Message = "Supported tables retrieved successfully",
                Data = tables
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supported tables");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred while getting supported tables",
                Data = null
            });
        }
    }
}