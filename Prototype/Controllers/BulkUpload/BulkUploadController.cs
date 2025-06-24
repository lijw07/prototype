using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Models;
using Prototype.DTOs;
using Prototype.DTOs.BulkUpload;
using Prototype.Services.BulkUpload;
using Prototype.Services.Interfaces;
using Prototype.Utility;
using Prototype.Helpers;

namespace Prototype.Controllers.BulkUpload
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BulkUploadController : ControllerBase
    {
        private readonly IBulkUploadService _bulkUploadService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ITableDetectionService _tableDetectionService;
        private readonly SentinelContext _context;
        private readonly ILogger<BulkUploadController> _logger;
        private readonly IValidationService _validationService;
        private readonly ITransactionService _transactionService;
        private readonly IAuthenticatedUserAccessor _userAccessor;

        public BulkUploadController(
            IBulkUploadService bulkUploadService,
            IFileStorageService fileStorageService,
            ITableDetectionService tableDetectionService,
            SentinelContext context,
            ILogger<BulkUploadController> logger,
            IValidationService validationService,
            ITransactionService transactionService,
            IAuthenticatedUserAccessor userAccessor)
        {
            _bulkUploadService = bulkUploadService;
            _fileStorageService = fileStorageService;
            _tableDetectionService = tableDetectionService;
            _context = context;
            _logger = logger;
            _validationService = validationService;
            _transactionService = transactionService;
            _userAccessor = userAccessor;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadBulkData([FromForm] BulkUploadRequest request)
        {
            try
            {
                var currentUser = await _userAccessor.GetCurrentUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "No file uploaded",
                        Data = null
                    });
                }

                var allowedExtensions = new[] { ".csv", ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid file format. Supported formats: CSV, XLSX, XLS",
                        Data = null
                    });
                }

                var uploadResult = await _transactionService.ExecuteInTransactionAsync(async () =>
                {
                    var fileData = await ReadFileDataAsync(request.File);
                    
                    var detectedTable = await _tableDetectionService.DetectTableTypeAsync(fileData, fileExtension);
                    if (detectedTable == null)
                    {
                        return Result<BulkUploadResponse>.Failure("Could not determine table type from file data");
                    }

                    var validationResult = await _bulkUploadService.ValidateDataAsync(fileData, detectedTable.TableType);
                    if (!validationResult.IsSuccess)
                    {
                        return Result<BulkUploadResponse>.Failure($"Validation failed: {validationResult.ErrorMessage}");
                    }

                    var processResult = await _bulkUploadService.ProcessBulkDataAsync(
                        fileData, 
                        detectedTable.TableType, 
                        currentUser.UserId,
                        request.IgnoreErrors);

                    if (request.SaveFile && processResult.IsSuccess)
                    {
                        var fileInfo = await _fileStorageService.SaveFileAsync(
                            request.File,
                            detectedTable.TableType,
                            currentUser.UserId);
                        
                        processResult.Data.SavedFilePath = fileInfo.FilePath;
                        processResult.Data.SavedFileId = fileInfo.FileId;
                    }

                    await LogBulkUploadActivity(currentUser.UserId, detectedTable.TableType, 
                        processResult.Data?.ProcessedRecords ?? 0, processResult.IsSuccess);

                    return processResult;
                });

                if (!uploadResult.IsSuccess)
                {
                    return BadRequest(new ApiResponse<BulkUploadResponse>
                    {
                        Success = false,
                        Message = uploadResult.ErrorMessage,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<BulkUploadResponse>
                {
                    Success = true,
                    Message = "Bulk upload completed successfully",
                    Data = uploadResult.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk upload");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred during bulk upload",
                    Data = null
                });
            }
        }

        [HttpGet("templates/{tableType}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUploadTemplate(string tableType)
        {
            try
            {
                var template = await _bulkUploadService.GetTemplateAsync(tableType);
                if (template == null)
                {
                    return NotFound(new ApiResponse<object>
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
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting the template",
                    Data = null
                });
            }
        }

        [HttpGet("supported-tables")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSupportedTables()
        {
            try
            {
                var tables = await _tableDetectionService.GetSupportedTablesAsync();
                return Ok(new ApiResponse<List<SupportedTableInfo>>
                {
                    Success = true,
                    Message = "Supported tables retrieved successfully",
                    Data = tables
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported tables");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting supported tables",
                    Data = null
                });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetUploadHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                var history = await _bulkUploadService.GetUploadHistoryAsync(currentUser.UserId, page, pageSize);
                return Ok(new ApiResponse<PaginatedResult<BulkUploadHistory>>
                {
                    Success = true,
                    Message = "Upload history retrieved successfully",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload history");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while getting upload history",
                    Data = null
                });
            }
        }

        [HttpDelete("file/{fileId}")]
        public async Task<IActionResult> DeleteUploadedFile(Guid fileId)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User not authenticated",
                        Data = null
                    });
                }

                var result = await _fileStorageService.DeleteFileAsync(fileId, currentUser.UserId);
                if (!result.IsSuccess)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.ErrorMessage,
                        Data = null
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "File deleted successfully",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting uploaded file {FileId}", fileId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while deleting the file",
                    Data = null
                });
            }
        }

        private async Task<byte[]> ReadFileDataAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private async Task<UserModel?> GetCurrentUserAsync()
        {
            return await _userAccessor.GetCurrentUserAsync(User);
        }

        private async Task LogBulkUploadActivity(Guid userId, string tableType, int recordCount, bool success)
        {
            var activityLog = new UserActivityLogModel
            {
                UserActivityLogId = Guid.NewGuid(),
                UserId = userId,
                DeviceInformation = HttpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown",
                ActionType = Prototype.Enum.ActionTypeEnum.Create,
                Description = $"Bulk upload to {tableType} table. Records: {recordCount}. Success: {success}",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            _context.UserActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }
}