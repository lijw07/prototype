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
        private readonly IProgressService _progressService;

        public BulkUploadService(
            SentinelContext context,
            IValidationService validationService,
            ITransactionService transactionService,
            ILogger<BulkUploadService> logger,
            ITableMappingService tableMappingService,
            IProgressService progressService)
        {
            _context = context;
            _validationService = validationService;
            _transactionService = transactionService;
            _logger = logger;
            _tableMappingService = tableMappingService;
            _progressService = progressService;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType, string fileExtension)
        {
            try
            {
                var dataTable = ParseFileToDataTable(fileData, fileExtension);
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
            string fileExtension,
            Guid userId, 
            bool ignoreErrors = false)
        {
            _logger.LogInformation("Starting ProcessBulkDataAsync for table: {TableType}, userId: {UserId}", tableType, userId);
            
            var stopwatch = Stopwatch.StartNew();
            var response = new BulkUploadResponse
            {
                TableType = tableType,
                ProcessedAt = DateTime.UtcNow,
                Errors = new List<BulkUploadError>()
            };

            try
            {
                var dataTable = ParseFileToDataTable(fileData, fileExtension);
                _logger.LogInformation("Parsed data table with {RowCount} rows", dataTable?.Rows.Count ?? 0);
                
                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    _logger.LogWarning("No data found in file");
                    return Result<BulkUploadResponse>.Failure("No data found in file");
                }

                response.TotalRecords = dataTable.Rows.Count;
                _logger.LogInformation("Total records to process: {TotalRecords}", response.TotalRecords);

                var mapper = _tableMappingService.GetMapper(tableType);
                if (mapper == null)
                {
                    _logger.LogError("No mapper found for table type: {TableType}", tableType);
                    return Result<BulkUploadResponse>.Failure($"No mapper found for table type: {tableType}");
                }
                
                _logger.LogInformation("Found mapper for table type: {TableType}", tableType);

                // Process rows directly without nested transaction since controller already manages transaction
                var rowNumber = 2;
                _logger.LogInformation("Starting to process {TotalRows} rows", dataTable.Rows.Count);
                
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
                                return Result<BulkUploadResponse>.Failure("Validation errors encountered");
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
                                        return Result<BulkUploadResponse>.Failure($"Error saving row {rowNumber}: {saveResult.ErrorMessage}");
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

                stopwatch.Stop();
                response.ProcessingTime = stopwatch.Elapsed;

                // Add history to context before saving
                await LogBulkUploadHistory(userId, tableType, response);

                // Save all changes at once (bulk data + history)
                _logger.LogInformation("Saving {ProcessedRecords} processed records to database", response.ProcessedRecords);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved all records to database");

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
                        Status = h.Status
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
                ".json" => ParseJsonToDataTable(fileData),
                ".xml" => ParseXmlToDataTable(fileData),
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

        private DataTable ParseJsonToDataTable(byte[] fileData)
        {
            var dataTable = new DataTable();

            try
            {
                var json = Encoding.UTF8.GetString(fileData);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(json);

                if (jsonDoc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array || 
                    jsonDoc.RootElement.GetArrayLength() == 0)
                {
                    return dataTable;
                }

                // Get columns from first object
                var firstElement = jsonDoc.RootElement[0];
                if (firstElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Add columns based on first object properties
                    foreach (var property in firstElement.EnumerateObject())
                    {
                        dataTable.Columns.Add(property.Name);
                    }

                    // Add rows for all objects in array
                    foreach (var element in jsonDoc.RootElement.EnumerateArray())
                    {
                        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var row = dataTable.NewRow();
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                if (element.TryGetProperty(column.ColumnName, out var propertyValue))
                                {
                                    row[column.ColumnName] = propertyValue.ValueKind switch
                                    {
                                        System.Text.Json.JsonValueKind.String => propertyValue.GetString() ?? string.Empty,
                                        System.Text.Json.JsonValueKind.Number => propertyValue.GetRawText(),
                                        System.Text.Json.JsonValueKind.True => "true",
                                        System.Text.Json.JsonValueKind.False => "false",
                                        System.Text.Json.JsonValueKind.Null => string.Empty,
                                        _ => propertyValue.GetRawText()
                                    };
                                }
                                else
                                {
                                    row[column.ColumnName] = string.Empty;
                                }
                            }
                            dataTable.Rows.Add(row);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON to DataTable");
            }

            return dataTable;
        }

        private DataTable ParseXmlToDataTable(byte[] fileData)
        {
            var dataTable = new DataTable();

            try
            {
                var xml = Encoding.UTF8.GetString(fileData);
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                
                var root = doc.Root;
                if (root == null || !root.Elements().Any())
                {
                    return dataTable;
                }

                // Get all unique element names from the first record to create columns
                var firstRecord = root.Elements().First();
                var columnNames = firstRecord.Elements()
                    .Select(e => e.Name.LocalName)
                    .Distinct()
                    .ToList();

                // Add columns to DataTable
                foreach (var columnName in columnNames)
                {
                    dataTable.Columns.Add(columnName);
                }

                // Add rows for each record
                foreach (var record in root.Elements())
                {
                    var row = dataTable.NewRow();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        var element = record.Elements()
                            .FirstOrDefault(e => e.Name.LocalName == column.ColumnName);
                        row[column.ColumnName] = element?.Value ?? string.Empty;
                    }
                    dataTable.Rows.Add(row);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing XML to DataTable");
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
            // Note: SaveChanges will be called by the controller's transaction
        }

        public async Task<Result<BulkUploadResponse>> ProcessBulkDataWithProgressAsync(
            byte[] fileData, 
            string tableType, 
            string fileExtension, 
            Guid userId, 
            string jobId,
            string? fileName = null,
            int fileIndex = 0, 
            int totalFiles = 1, 
            bool ignoreErrors = false)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting ProcessBulkDataWithProgressAsync for table: {TableType}, userId: {UserId}, jobId: {JobId}", tableType, userId, jobId);

            try
            {
                // Parse data and get total count for progress tracking
                var dataTable = ParseFileToDataTable(fileData, fileExtension);
                var totalRecords = dataTable.Rows.Count;
                
                _logger.LogInformation("Parsed data table with {TotalRecords} rows for job {JobId}", totalRecords, jobId);

                // Initial progress update - parsing complete
                var baseProgress = (double)fileIndex / totalFiles * 100;
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (5.0 / totalFiles), // 5% for parsing
                    Status = "Parsing Complete",
                    CurrentOperation = $"Processing {fileName ?? "file"}",
                    ProcessedRecords = 0,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles
                });

                var mapper = _tableMappingService.GetMapper(tableType);
                if (mapper == null)
                {
                    await _progressService.NotifyError(jobId, $"No mapper found for table type: {tableType}");
                    return Result<BulkUploadResponse>.Failure($"No mapper found for table type: {tableType}");
                }

                _logger.LogInformation("Found mapper for table type: {TableType}", tableType);

                var response = new BulkUploadResponse
                {
                    TotalRecords = totalRecords,
                    ProcessedRecords = 0,
                    FailedRecords = 0,
                    TableType = tableType,
                    ProcessedAt = DateTime.UtcNow,
                    Errors = new List<BulkUploadError>(),
                    FileName = fileName
                };

                // Progress tracking variables
                var processedRecords = 0;
                var validationErrors = new List<BulkUploadError>();
                
                // Phase 1: Validation (takes about 30% of time)
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (10.0 / totalFiles),
                    Status = "Validating",
                    CurrentOperation = "Validating data records",
                    ProcessedRecords = 0,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles
                });

                _logger.LogInformation("Starting validation for {TotalRecords} records", totalRecords);

                // Validate all rows first
                var rowNumber = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        var validationResult = await mapper.ValidateRowAsync(row, rowNumber);
                        if (!validationResult.IsValid)
                        {
                            foreach (var error in validationResult.Errors)
                            {
                                validationErrors.Add(new BulkUploadError
                                {
                                    RowNumber = rowNumber,
                                    ErrorMessage = error,
                                    FileName = fileName
                                });
                            }
                            if (!ignoreErrors)
                            {
                                response.FailedRecords++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        validationErrors.Add(new BulkUploadError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"Validation error - {ex.Message}",
                            FileName = fileName
                        });
                        _logger.LogError(ex, "Validation error for row {RowNumber}", rowNumber);
                        if (!ignoreErrors)
                        {
                            response.FailedRecords++;
                        }
                    }

                    rowNumber++;
                    processedRecords++;

                    // Update progress every 10 records or every 5% of total
                    if (processedRecords % Math.Max(1, totalRecords / 20) == 0 || processedRecords % 10 == 0)
                    {
                        var validationProgress = (double)processedRecords / totalRecords * 30; // 30% for validation
                        await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                        {
                            JobId = jobId,
                            ProgressPercentage = baseProgress + (10.0 / totalFiles) + (validationProgress / totalFiles),
                            Status = "Validating",
                            CurrentOperation = $"Validated {processedRecords}/{totalRecords} records",
                            ProcessedRecords = processedRecords,
                            TotalRecords = totalRecords,
                            CurrentFileName = fileName,
                            ProcessedFiles = fileIndex,
                            TotalFiles = totalFiles,
                            Errors = validationErrors.Count > 0 ? validationErrors.TakeLast(5).Select(e => e.ErrorMessage).ToList() : null
                        });
                    }
                }

                response.Errors = validationErrors;

                // Check if we should continue based on validation results
                if (!ignoreErrors && response.FailedRecords > 0)
                {
                    await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                    {
                        JobId = jobId,
                        ProgressPercentage = baseProgress + (40.0 / totalFiles),
                        Status = "Validation Failed",
                        CurrentOperation = $"Validation failed with {response.FailedRecords} errors",
                        ProcessedRecords = processedRecords,
                        TotalRecords = totalRecords,
                        CurrentFileName = fileName,
                        ProcessedFiles = fileIndex,
                        TotalFiles = totalFiles,
                        Errors = validationErrors.TakeLast(10).Select(e => e.ErrorMessage).ToList()
                    });

                    response.ProcessingTime = stopwatch.Elapsed;
                    return Result<BulkUploadResponse>.Failure($"Validation failed: {string.Join("; ", validationErrors.Select(e => e.ErrorMessage))}");
                }

                // Phase 2: Data Processing (takes about 60% of time)
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (40.0 / totalFiles),
                    Status = "Processing",
                    CurrentOperation = "Saving data to database",
                    ProcessedRecords = 0,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles
                });

                _logger.LogInformation("Starting to process {TotalRecords} rows", totalRecords);

                // Process and save data
                var successfullyProcessed = 0;
                rowNumber = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        // Skip rows that failed validation if not ignoring errors
                        var validationResult = await mapper.ValidateRowAsync(row, rowNumber);
                        if (!validationResult.IsValid && !ignoreErrors)
                        {
                            rowNumber++;
                            continue;
                        }

                        var saveResult = await mapper.SaveRowAsync(row, userId);
                        if (saveResult.IsSuccess)
                        {
                            successfullyProcessed++;
                        }
                        else
                        {
                            response.FailedRecords++;
                            response.Errors.Add(new BulkUploadError
                            {
                                RowNumber = rowNumber,
                                ErrorMessage = saveResult.ErrorMessage,
                                FileName = fileName
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailedRecords++;
                        response.Errors.Add(new BulkUploadError
                        {
                            RowNumber = rowNumber,
                            ErrorMessage = $"Processing error - {ex.Message}",
                            FileName = fileName
                        });
                        _logger.LogError(ex, "Processing error for row {RowNumber}", rowNumber);
                    }

                    rowNumber++;

                    // Update progress every 5 records or every 2% of total
                    if (successfullyProcessed % Math.Max(1, totalRecords / 50) == 0 || successfullyProcessed % 5 == 0)
                    {
                        var processingProgress = (double)successfullyProcessed / totalRecords * 60; // 60% for processing
                        await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                        {
                            JobId = jobId,
                            ProgressPercentage = baseProgress + (40.0 / totalFiles) + (processingProgress / totalFiles),
                            Status = "Processing",
                            CurrentOperation = $"Processed {successfullyProcessed}/{totalRecords} records",
                            ProcessedRecords = successfullyProcessed,
                            TotalRecords = totalRecords,
                            CurrentFileName = fileName,
                            ProcessedFiles = fileIndex,
                            TotalFiles = totalFiles,
                            Errors = response.Errors.Count > 0 ? response.Errors.TakeLast(3).Select(e => e.ErrorMessage).ToList() : null
                        });
                    }
                }

                response.ProcessedRecords = successfullyProcessed;

                // Final save to database
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (95.0 / totalFiles),
                    Status = "Saving",
                    CurrentOperation = "Saving changes to database",
                    ProcessedRecords = successfullyProcessed,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles
                });

                if (response.ProcessedRecords > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved all records to database for job {JobId}", jobId);
                }

                stopwatch.Stop();
                response.ProcessingTime = stopwatch.Elapsed;

                // Log to history
                await LogBulkUploadHistory(userId, tableType, response);

                // Final progress update for this file
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (100.0 / totalFiles),
                    Status = "Completed",
                    CurrentOperation = $"File {fileName ?? "upload"} completed successfully",
                    ProcessedRecords = response.ProcessedRecords,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex + 1,
                    TotalFiles = totalFiles
                });

                _logger.LogInformation("ProcessBulkDataWithProgressAsync completed for job {JobId}. Processed: {ProcessedRecords}, Failed: {FailedRecords}", 
                    jobId, response.ProcessedRecords, response.FailedRecords);

                return Result<BulkUploadResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error in ProcessBulkDataWithProgressAsync for job {JobId}", jobId);
                
                await _progressService.NotifyError(jobId, $"Processing error: {ex.Message}");
                
                return Result<BulkUploadResponse>.Failure($"Error processing bulk data: {ex.Message}");
            }
        }
    }
}