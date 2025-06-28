using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload;

public class BulkUploadService : IBulkUploadService
{
    private readonly SentinelContext _context;
    private readonly ILogger<BulkUploadService> _logger;
    private readonly ITableMappingService _tableMappingService;
    private readonly IFileParsingService _fileParsingService;
    private readonly IBulkValidationService _validationService;
    private readonly IBulkDataProcessingService _dataProcessingService;

    public BulkUploadService(
        SentinelContext context,
        ILogger<BulkUploadService> logger,
        ITableMappingService tableMappingService,
        IFileParsingService fileParsingService,
        IBulkValidationService validationService,
        IBulkDataProcessingService dataProcessingService)
    {
        _context = context;
        _logger = logger;
        _tableMappingService = tableMappingService;
        _fileParsingService = fileParsingService;
        _validationService = validationService;
        _dataProcessingService = dataProcessingService;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType, string fileExtension, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataTable = _fileParsingService.ParseFileToDataTable(fileData, fileExtension);
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return Result<bool>.Failure("No data found in file");
            }

            return await _validationService.ValidateDataAsync(dataTable, tableType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating bulk upload data");
            return Result<bool>.Failure($"Validation error: {ex.Message}");
        }
    }

    public async Task<Result<BulkUploadResponseDto>> ProcessBulkDataAsync(
        byte[] fileData, 
        string tableType, 
        string fileExtension,
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ProcessBulkDataAsync for table: {TableType}, userId: {UserId}", tableType, userId);

        try
        {
            var dataTable = _fileParsingService.ParseFileToDataTable(fileData, fileExtension);
            _logger.LogInformation("Parsed data table with {RowCount} rows", dataTable?.Rows.Count ?? 0);
            
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                _logger.LogWarning("No data found in file");
                return Result<BulkUploadResponseDto>.Failure("No data found in file");
            }

            return await _dataProcessingService.ProcessDataAsync(dataTable, tableType, userId, ignoreErrors, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload");
            return Result<BulkUploadResponseDto>.Failure($"Processing error: {ex.Message}");
        }
    }

    public async Task<Result<BulkUploadResponseDto>> ProcessBulkDataWithProgressAsync(
        byte[] fileData, 
        string tableType, 
        string fileExtension,
        Guid userId,
        string jobId,
        string? fileName = null,
        int fileIndex = 0,
        int totalFiles = 1,
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ProcessBulkDataWithProgressAsync for table: {TableType}, userId: {UserId}", tableType, userId);

        try
        {
            var dataTable = _fileParsingService.ParseFileToDataTable(fileData, fileExtension);
            _logger.LogInformation("Parsed data table with {RowCount} rows", dataTable?.Rows.Count ?? 0);
            
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                _logger.LogWarning("No data found in file");
                return Result<BulkUploadResponseDto>.Failure("No data found in file");
            }

            return await _dataProcessingService.ProcessDataWithProgressAsync(dataTable, tableType, userId, ignoreErrors, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload with progress");
            return Result<BulkUploadResponseDto>.Failure($"Processing error: {ex.Message}");
        }
    }

    public async Task<FileTemplateInfo?> GetTemplateAsync(string tableType)
    {
        try
        {
            var mapper = _tableMappingService.GetMapper(tableType);
            if (mapper == null)
            {
                return null;
            }

            var columns = mapper.GetTemplateColumns();
            var exampleData = mapper.GetExampleData();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(tableType);

            // Add headers
            for (int i = 0; i < columns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = columns[i].ColumnName;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                
                // Add column description as comment
                if (!string.IsNullOrEmpty(columns[i].Description))
                {
                    worksheet.Cells[1, i + 1].AddComment(columns[i].Description, "Template");
                }
            }

            // Add example data
            if (exampleData != null && exampleData.Any())
            {
                var rowIndex = 2;
                foreach (var example in exampleData)
                {
                    for (int i = 0; i < columns.Count; i++)
                    {
                        var columnName = columns[i].ColumnName;
                        if (example.ContainsKey(columnName))
                        {
                            worksheet.Cells[rowIndex, i + 1].Value = example[columnName];
                        }
                    }
                    rowIndex++;
                }
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var templateData = package.GetAsByteArray();
            var fileName = $"{tableType}_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            return new FileTemplateInfo
            {
                FileName = fileName,
                Content = templateData,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating template for table type: {TableType}", tableType);
            return null;
        }
    }

    public async Task<PaginatedResult<BulkUploadHistory>> GetUploadHistoryAsync(Guid userId, int page, int pageSize)
    {
        try
        {
            var query = _context.BulkUploadHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.UploadedAt);

            var totalCount = await query.CountAsync();
            var historyModels = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var histories = historyModels.Select(h => new BulkUploadHistory
            {
                UploadId = h.UploadId,
                FileName = h.FileName,
                TableType = h.TableType,
                TotalRecords = h.TotalRecords,
                ProcessedRecords = h.ProcessedRecords,
                FailedRecords = h.FailedRecords,
                UploadedAt = h.UploadedAt,
                Status = h.Status,
                UploadedBy = "System" // TODO: Add user name lookup
            }).ToList();

            return new PaginatedResult<BulkUploadHistory>
            {
                Items = histories,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upload history for user: {UserId}", userId);
            return new PaginatedResult<BulkUploadHistory>
            {
                Items = new List<BulkUploadHistory>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 0
            };
        }
    }
}