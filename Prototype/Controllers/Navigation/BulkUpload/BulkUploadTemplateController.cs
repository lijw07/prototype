using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Exceptions;
using Prototype.Services.BulkUpload;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/templates")]
public class BulkUploadTemplateController(
    ILogger<BulkUploadTemplateController> logger,
    SentinelContext context,
    IBulkUploadService bulkUploadService,
    ITableDetectionService tableDetectionService)
    : BaseNavigationController(logger, context)
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
    private readonly ITableDetectionService _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));

    [HttpGet("{tableType}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUploadTemplate(string tableType)
    {
        try
        {
            var template = await _bulkUploadService.GetTemplateAsync(tableType);
            if (template == null)
            {
                return NotFound($"Template not found for table type: {tableType}");
            }

            return File(template.Content, template.ContentType, template.FileName);
        }
        catch (DataNotFoundException ex)
        {
            Logger.LogWarning(ex, "Template not found for table type: {TableType}", tableType);
            return NotFound($"Template not found for table type: {tableType}");
        }
        catch (BusinessLogicException ex)
        {
            Logger.LogWarning(ex, "Business logic error getting template for {TableType}", tableType);
            return BadRequestWithMessage(ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            Logger.LogError(ex, "External service failure getting template for {TableType}", tableType);
            return StatusCode(503, "Template service temporarily unavailable");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error getting upload template for {TableType}", tableType);
            return InternalServerError("An unexpected error occurred while getting the template");
        }
    }

    [HttpGet("supported")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSupportedTables()
    {
        try
        {
            var tables = await _tableDetectionService.GetSupportedTablesAsync();
            return SuccessResponse(tables, "Supported tables retrieved successfully");
        }
        catch (ExternalServiceException ex)
        {
            Logger.LogError(ex, "External service failure getting supported tables");
            return StatusCode(503, "Table detection service temporarily unavailable");
        }
        catch (DataNotFoundException ex)
        {
            Logger.LogWarning(ex, "No supported tables found");
            return SuccessResponse(new List<SupportedTableInfoDto>(), "No supported tables found");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error getting supported tables");
            return InternalServerError("An unexpected error occurred while getting supported tables");
        }
    }
}