using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;
using Prototype.Services.Interfaces;
using System.Diagnostics;

namespace Prototype.Services.BulkUpload
{
    public class BulkUploadService : IBulkUploadService
    {
        private readonly SentinelContext _context;
        private readonly IValidationService _validationService;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<BulkUploadService> _logger;
        private readonly ITableMappingService _tableMappingService;

        public BulkUploadService(
            SentinelContext context,
            IValidationService validationService,
            ITransactionService transactionService,
            ILogger<BulkUploadService> logger,
            ITableMappingService tableMappingService)
        {
            _context = context;
            _validationService = validationService;
            _transactionService = transactionService;
            _logger = logger;
            _tableMappingService = tableMappingService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType)
        {
            try
            {
                var dataTable = ParseFileToDataTable(fileData, Path.GetExtension(tableType));
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    return Result<bool>.Failure("No data found in file");
                }

                var mapper = _tableMappingService.GetMapper(tableType);
                if (mapper == null)
                {
                    return Result<bool>.Failure($"No mapper found for table type: {tableType}");
                }

                var validationErrors = new List<string>();
                var rowNumber = 2; // Start from 2 assuming row 1 is header

                foreach (DataRow row in dataTable.Rows)
                {
                    var validationResult = await mapper.ValidateRowAsync(row, rowNumber);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.AddRange(validationResult.Errors);
                    }
                    rowNumber++;
                }

                if (validationErrors.Any())
                {
                    return Result<bool>.Failure($"Validation errors found: {string.Join("; ", validationErrors.Take(10))}");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating bulk upload data");
                return Result<bool>.Failure($"Validation error: {ex.Message}");
            }
        }

        public async Task<Result<BulkUploadResponse>> ProcessBulkDataAsync(
            byte[] fileData, 
            string tableType, 
            Guid userId, 
            bool ignoreErrors = false)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new BulkUploadResponse
            {
                TableType = tableType,
                ProcessedAt = DateTime.UtcNow,
                Errors = new List<BulkUploadError>()
            };

            try
            {
                var dataTable = ParseFileToDataTable(fileData, Path.GetExtension(tableType));
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    return Result<BulkUploadResponse>.Failure("No data found in file");
                }

                response.TotalRecords = dataTable.Rows.Count;

                var mapper = _tableMappingService.GetMapper(tableType);
                if (mapper == null)
                {
                    return Result<BulkUploadResponse>.Failure($"No mapper found for table type: {tableType}");
                }

                var processResult = await _transactionService.ExecuteInTransactionAsync(async () =>
                {
                    var rowNumber = 2;
                    foreach (DataRow row in dataTable.Rows)
                    {
                        try
                        {
                            var validationResult = await mapper.ValidateRowAsync(row, rowNumber);
                            if (!validationResult.IsValid)
                            {
                                response.FailedRecords++;
                                foreach (var error in validationResult.Errors)
                                {
                                    response.Errors.Add(new BulkUploadError
                                    {
                                        RowNumber = rowNumber,
                                        ErrorMessage = error,
                                        RowData = GetRowData(row)
                                    });
                                }

                                if (!ignoreErrors)
                                {
                                    return Result<bool>.Failure("Validation errors encountered");
                                }
                            }
                            else
                            {
                                var saveResult = await mapper.SaveRowAsync(row, userId);
                                if (saveResult.IsSuccess)
                                {
                                    response.ProcessedRecords++;
                                }
                                else
                                {
                                    response.FailedRecords++;
                                    response.Errors.Add(new BulkUploadError
                                    {
                                        RowNumber = rowNumber,
                                        ErrorMessage = saveResult.ErrorMessage,
                                        RowData = GetRowData(row)
                                    });

                                    if (!ignoreErrors)
                                    {
                                        return Result<bool>.Failure($"Error saving row {rowNumber}: {saveResult.ErrorMessage}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            response.FailedRecords++;
                            response.Errors.Add(new BulkUploadError
                            {
                                RowNumber = rowNumber,
                                ErrorMessage = ex.Message,
                                RowData = GetRowData(row)
                            });

                            if (!ignoreErrors)
                            {
                                throw;
                            }
                        }
                        rowNumber++;
                    }

                    return Result<bool>.Success(true);
                });

                if (!processResult.IsSuccess && !ignoreErrors)
                {
                    return Result<BulkUploadResponse>.Failure(processResult.ErrorMessage);
                }

                stopwatch.Stop();
                response.ProcessingTime = stopwatch.Elapsed;

                await LogBulkUploadHistory(userId, tableType, response);

                return Result<BulkUploadResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk upload");
                return Result<BulkUploadResponse>.Failure($"Processing error: {ex.Message}");
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
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var fileContent = await package.GetAsByteArrayAsync();
                return new FileTemplateInfo
                {
                    Content = fileContent,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileName = $"{tableType}_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating template for {TableType}", tableType);
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
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new BulkUploadHistory
                    {
                        UploadId = h.UploadId,
                        FileName = h.FileName,
                        TableType = h.TableType,
                        TotalRecords = h.TotalRecords,
                        ProcessedRecords = h.ProcessedRecords,
                        FailedRecords = h.FailedRecords,
                        UploadedAt = h.UploadedAt,
                        UploadedBy = h.User.Username,
                        Status = h.Status,
                        HasSavedFile = h.SavedFileId.HasValue,
                        SavedFileId = h.SavedFileId
                    })
                    .ToListAsync();

                return new PaginatedResult<BulkUploadHistory>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload history for user {UserId}", userId);
                throw;
            }
        }

        private DataTable? ParseFileToDataTable(byte[] fileData, string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".csv" => ParseCsvToDataTable(fileData),
                ".xlsx" or ".xls" => ParseExcelToDataTable(fileData),
                _ => null
            };
        }

        private DataTable ParseCsvToDataTable(byte[] fileData)
        {
            var dataTable = new DataTable();
            
            using var memoryStream = new MemoryStream(fileData);
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null
            });

            using var dr = new CsvDataReader(csv);
            dataTable.Load(dr);

            return dataTable;
        }

        private DataTable ParseExcelToDataTable(byte[] fileData)
        {
            var dataTable = new DataTable();

            using var memoryStream = new MemoryStream(fileData);
            using var package = new ExcelPackage(memoryStream);
            
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null || worksheet.Dimension == null)
            {
                return dataTable;
            }

            // Add columns
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                var columnName = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
                dataTable.Columns.Add(columnName);
            }

            // Add rows
            for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
            {
                var dataRow = dataTable.NewRow();
                for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                {
                    dataRow[col - 1] = worksheet.Cells[row, col].Value?.ToString() ?? string.Empty;
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        }

        private Dictionary<string, object> GetRowData(DataRow row)
        {
            var data = new Dictionary<string, object>();
            foreach (DataColumn column in row.Table.Columns)
            {
                data[column.ColumnName] = row[column] ?? DBNull.Value;
            }
            return data;
        }

        private async Task LogBulkUploadHistory(Guid userId, string tableType, BulkUploadResponse response)
        {
            var history = new BulkUploadHistoryModel
            {
                UploadId = Guid.NewGuid(),
                UserId = userId,
                TableType = tableType,
                FileName = "BulkUpload", // This should be passed from controller
                TotalRecords = response.TotalRecords,
                ProcessedRecords = response.ProcessedRecords,
                FailedRecords = response.FailedRecords,
                UploadedAt = response.ProcessedAt,
                Status = response.FailedRecords == 0 ? "Success" : response.ProcessedRecords == 0 ? "Failed" : "Partial",
                ProcessingTime = response.ProcessingTime,
                ErrorDetails = response.Errors.Any() ? System.Text.Json.JsonSerializer.Serialize(response.Errors) : null
            };

            _context.BulkUploadHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
}