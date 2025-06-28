using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Common.Responses;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Exceptions;
using Prototype.Helpers;
using Prototype.Services;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Authorize]
[Route("bulk-upload/core")]
[ApiController]
public class BulkUploadCoreController(
    IBulkUploadService bulkUploadService,
    ITableDetectionService tableDetectionService,
    IAuthenticatedUserAccessor userAccessor,
    SentinelContext context,
    ILogger<BulkUploadCoreController> logger,
    TransactionService transactionService)
    : BaseNavigationController(logger, context, userAccessor, transactionService)
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
    private readonly ITableDetectionService _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
    private readonly ITransactionService _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor;

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBulkData([FromForm] BulkUploadRequestDto requestDto)
    {
        Logger.LogInformation("BulkUploadCoreController.UploadBulkData called with file: {FileName}", requestDto?.File?.FileName ?? "null");
        
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return HandleUserNotAuthenticated();
            }

            if (requestDto.File == null || requestDto.File.Length == 0)
            {
                return BadRequestWithMessage("No file uploaded");
            }

            if (!ValidateFileExtension(requestDto.File.FileName, out var fileExtension))
            {
                return BadRequestWithMessage("Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS");
            }

            var uploadResult = await _transactionService.ExecuteInTransactionAsync(async () =>
            {
                var fileData = await ReadFileDataAsync(requestDto.File);
                
                var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                if (detectedTable == null)
                {
                    return Result<BulkUploadResponseDto>.Failure("Could not determine table type from file data");
                }

                var validationResult = await _bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
                if (!validationResult.IsSuccess)
                {
                    return Result<BulkUploadResponseDto>.Failure($"Validation failed: {validationResult.ErrorMessage}");
                }

                var processResult = await _bulkUploadService.ProcessBulkDataAsync(
                    fileData, 
                    detectedTable.TableType, 
                    fileExtension,
                    currentUser.UserId,
                    requestDto.IgnoreErrors);

                await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                    processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

                return processResult;
            });

            if (!uploadResult.IsSuccess)
            {
                var errorResponse = ApiResponse<BulkUploadResponseDto>.BadRequest(uploadResult.ErrorMessage);
                
                Logger.LogWarning("Returning error bulk upload response: {Response}", 
                    System.Text.Json.JsonSerializer.Serialize(errorResponse));
                
                return BadRequest(errorResponse);
            }

            // Add file context to response
            if (uploadResult.Data != null)
            {
                uploadResult.Data.FileName = requestDto.File.FileName;
                uploadResult.Data.FileIndex = requestDto.FileIndex;
                uploadResult.Data.TotalFiles = requestDto.TotalFiles;
            }

            var response = ApiResponse<BulkUploadResponseDto>.Success(uploadResult.Data, "Bulk upload completed successfully");
            
            Logger.LogInformation("Returning successful bulk upload response: {Response}", 
                System.Text.Json.JsonSerializer.Serialize(response));
            
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            Logger.LogWarning(ex, "Validation failed during bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return BadRequest(ApiResponse<object>.ValidationError(ex.ValidationErrors, ex.FieldErrors));
        }
        catch (DataNotFoundException ex)
        {
            Logger.LogWarning(ex, "Required data not found during bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return BadRequest(ApiResponse<object>.NotFound(ex.Message));
        }
        catch (BusinessLogicException ex)
        {
            Logger.LogWarning(ex, "Business rule violation during bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return BadRequest(ApiResponse<object>.BadRequest(ex.Message));
        }
        catch (ExternalServiceException ex)
        {
            Logger.LogError(ex, "External service failure during bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return StatusCode(503, ApiResponse<object>.InternalServerError("File processing service temporarily unavailable"));
        }
        catch (AuthorizationException ex)
        {
            Logger.LogWarning(ex, "Authorization failed during bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error in bulk upload for file: {FileName}", requestDto?.File?.FileName);
            return StatusCode(500, ApiResponse<object>.InternalServerError("An unexpected error occurred during bulk upload"));
        }
    }
}