using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Prototype.Common.Responses;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Services.BulkUpload;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation.BulkUpload;

[Route("bulk-upload/multiple")]
public class BulkUploadMultipleController(
    ILogger<BulkUploadMultipleController> logger,
    IAuthenticatedUserAccessor userAccessor,
    SentinelContext context,
    IBulkUploadService bulkUploadService,
    ITableDetectionService tableDetectionService)
    : BaseNavigationController(logger, context, userAccessor)
{
    private readonly IBulkUploadService _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
    private readonly ITableDetectionService _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
    private readonly IAuthenticatedUserAccessor _userAccessor = userAccessor;

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultipleBulkData([FromForm] MultipleBulkUploadRequestDto requestDto)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
            {
                return HandleUserNotAuthenticated();
            }

            if (requestDto.Files.IsNullOrEmpty())
            {
                return BadRequestWithMessage("No files uploaded");
            }

            // Validate all files first
            foreach (var file in requestDto.Files)
            {
                if (!ValidateFileExtension(file.FileName, out _))
                {
                    return BadRequestWithMessage($"Invalid file format for {file.FileName}. Supported formats: CSV, XML, JSON, XLSX, XLS");
                }
            }

            var response = new MultipleBulkUploadResponseDto
            {
                TotalFiles = requestDto.Files.Count,
                ProcessedAt = DateTime.UtcNow
            };

            var overallStartTime = DateTime.UtcNow;
            var processedFiles = 0;
            var failedFiles = 0;
            
            for (int i = 0; i < requestDto.Files.Count; i++)
            {
                var file = requestDto.Files[i];
                try
                {
                    var fileResult = await ProcessSingleFileAsync(file, currentUser, requestDto.IgnoreErrors, i, requestDto.Files.Count);
                    response.FileResults.Add(fileResult);
                    
                    if (fileResult.ProcessedRecords > 0)
                    {
                        processedFiles++;
                        response.ProcessedRecords += fileResult.ProcessedRecords;
                    }
                    
                    if (fileResult.FailedRecords > 0)
                    {
                        response.FailedRecords += fileResult.FailedRecords;
                    }
                    
                    response.TotalRecords += fileResult.TotalRecords;
                }
                catch (Exception ex)
                {
                    failedFiles++;
                    Logger.LogError(ex, "Error processing file {FileName}", file.FileName);
                    
                    response.GlobalErrors.Add($"Failed to process {file.FileName}: {ex.Message}");
                    
                    if (!requestDto.ContinueOnError)
                    {
                        break;
                    }
                }
            }

            response.ProcessedFiles = processedFiles;
            response.FailedFiles = failedFiles;
            response.TotalProcessingTime = DateTime.UtcNow - overallStartTime;
            response.OverallSuccess = failedFiles == 0 && response.GlobalErrors.Count == 0;

            // Log the overall activity
            await LogMultipleBulkUploadActivity(currentUser.UserId, response);

            return SuccessResponse(response, response.OverallSuccess 
                    ? "Multiple file bulk upload completed successfully" 
                    : "Multiple file bulk upload completed with some errors");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in multiple file bulk upload");
            return StatusCode(500, ApiResponse<object>.InternalServerError("An error occurred during multiple file bulk upload"));
        }
    }

    private async Task<BulkUploadResponseDto> ProcessSingleFileAsync(IFormFile file, UserModel currentUser, bool ignoreErrors, int fileIndex, int totalFiles)
    {
        var fileData = await ReadFileDataAsync(file);
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
        if (detectedTable == null)
        {
            throw new InvalidOperationException($"Could not determine table type from file data for {file.FileName}");
        }

        var validationResult = await _bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType, fileExtension);
        if (!validationResult.IsSuccess)
        {
            throw new InvalidOperationException($"Validation failed for {file.FileName}: {validationResult.ErrorMessage}");
        }

        var processResult = await _bulkUploadService.ProcessBulkDataAsync(
            fileData, 
            detectedTable.TableType, 
            fileExtension,
            currentUser.UserId,
            ignoreErrors);

        if (!processResult.IsSuccess)
        {
            throw new InvalidOperationException($"Processing failed for {file.FileName}: {processResult.ErrorMessage}");
        }

        // Add file context to the result
        if (processResult.Data != null)
        {
            processResult.Data.FileName = file.FileName;
            processResult.Data.FileIndex = fileIndex;
            processResult.Data.TotalFiles = totalFiles;
            
            // Add file name to all errors
            foreach (var error in processResult.Data.Errors)
            {
                error.FileName = file.FileName;
            }
        }

        await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
            processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

        return processResult.Data ?? new BulkUploadResponseDto();
    }
}