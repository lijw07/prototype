using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;
using Prototype.Utility;

namespace Prototype.Controllers.BulkUpload;

[Authorize]
[Route("api/bulkupload/core")]
[ApiController]
public class BulkUploadCoreController : BaseBulkUploadController
{
    private readonly IBulkUploadService _bulkUploadService;
    private readonly ITableDetectionService _tableDetectionService;
    private readonly ITransactionService _transactionService;

    public BulkUploadCoreController(
        IBulkUploadService bulkUploadService,
        ITableDetectionService tableDetectionService,
        SentinelContext context,
        ILogger<BulkUploadCoreController> logger,
        ITransactionService transactionService)
        : base(context, logger)
    {
        _bulkUploadService = bulkUploadService ?? throw new ArgumentNullException(nameof(bulkUploadService));
        _tableDetectionService = tableDetectionService ?? throw new ArgumentNullException(nameof(tableDetectionService));
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadBulkData([FromForm] BulkUploadRequestDto requestDto)
    {
        Logger.LogInformation("BulkUploadCoreController.UploadBulkData called with file: {FileName}", requestDto?.File?.FileName ?? "null");
        
        try
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Unauthorized(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Admin user not found",
                    Data = null
                });
            }

            if (requestDto.File == null || requestDto.File.Length == 0)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "No file uploaded",
                    Data = null
                });
            }

            if (!ValidateFileExtension(requestDto.File.FileName, out var fileExtension))
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid file format. Supported formats: CSV, XML, JSON, XLSX, XLS",
                    Data = null
                });
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
                var errorResponse = new ApiResponseDto<BulkUploadResponseDto>
                {
                    Success = false,
                    Message = uploadResult.ErrorMessage,
                    Data = null
                };
                
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

            var response = new ApiResponseDto<BulkUploadResponseDto>
            {
                Success = true,
                Message = "Bulk upload completed successfully",
                Data = uploadResult.Data
            };
            
            Logger.LogInformation("Returning successful bulk upload response: {Response}", 
                System.Text.Json.JsonSerializer.Serialize(response));
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in bulk upload");
            return StatusCode(500, new ApiResponseDto<object>
            {
                Success = false,
                Message = "An error occurred during bulk upload",
                Data = null
            });
        }
    }
}