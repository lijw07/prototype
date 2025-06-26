using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Services.Interfaces;
using System.Diagnostics;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload;

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

    public async Task<Result<bool>> ValidateDataAsync(byte[] fileData, string tableType, string fileExtension, CancellationToken cancellationToken = default)
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
            
            _logger.LogInformation("Starting validation for {TotalRecords} records", dataTable.Rows.Count);

            // Use batch validation if supported for better performance
            if (mapper is IBatchTableMapper batchMapper)
            {
                _logger.LogInformation("Using optimized batch validation for {TotalRecords} records", dataTable.Rows.Count);
                
                try
                {
                    var validationResults = await batchMapper.ValidateBatchAsync(dataTable, cancellationToken);
                    
                    // Process validation results
                    foreach (var (rowNum, result) in validationResults)
                    {
                        if (!result.IsValid)
                        {
                            validationErrors.AddRange(result.Errors);
                        }
                    }
                    
                    _logger.LogInformation("Batch validation completed. Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                        validationResults.Count(vr => vr.Value.IsValid), validationResults.Count(vr => !vr.Value.IsValid));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch validation failed, falling back to row-by-row validation");
                    // Fall back to individual validation if batch fails
                    validationErrors.Clear();
                }
            }
            
            // Fall back to row-by-row validation if batch not supported or failed
            if (!validationErrors.Any() && !(mapper is IBatchTableMapper))
            {
                _logger.LogInformation("Using row-by-row validation for {TotalRecords} records", dataTable.Rows.Count);
                
                var rowNumber = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var validationResult = await mapper.ValidateRowAsync(row, rowNumber, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.AddRange(validationResult.Errors);
                    }
                    rowNumber++;
                }
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

    public async Task<Result<BulkUploadResponseDto>> ProcessBulkDataAsync(
        byte[] fileData, 
        string tableType, 
        string fileExtension,
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ProcessBulkDataAsync for table: {TableType}, userId: {UserId}", tableType, userId);
        
        var stopwatch = Stopwatch.StartNew();
        var response = new BulkUploadResponseDto
        {
            TableType = tableType,
            ProcessedAt = DateTime.UtcNow,
            Errors = new List<BulkUploadErrorDto>()
        };

        try
        {
            var dataTable = ParseFileToDataTable(fileData, fileExtension);
            _logger.LogInformation("Parsed data table with {RowCount} rows", dataTable?.Rows.Count ?? 0);
            
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                _logger.LogWarning("No data found in file");
                return Result<BulkUploadResponseDto>.Failure("No data found in file");
            }

            response.TotalRecords = dataTable.Rows.Count;
            _logger.LogInformation("Total records to process: {TotalRecords}", response.TotalRecords);

            var mapper = _tableMappingService.GetMapper(tableType);
            if (mapper == null)
            {
                _logger.LogError("No mapper found for table type: {TableType}", tableType);
                return Result<BulkUploadResponseDto>.Failure($"No mapper found for table type: {tableType}");
            }
            
            _logger.LogInformation("Found mapper for table type: {TableType}", tableType);

            // Use optimized batch processing for better performance
            var totalRecords = dataTable.Rows.Count;
            var validationResults = new Dictionary<int, ValidationResultDto>();
            var validationErrors = new List<BulkUploadErrorDto>();
            
            _logger.LogInformation("Starting validation for {TotalRecords} records", totalRecords);

            // Phase 1: Validation - Use batch validation if supported
            if (mapper is IBatchTableMapper batchMapper)
            {
                _logger.LogInformation("Using optimized batch validation for {TotalRecords} records", totalRecords);
                
                try
                {
                    validationResults = await batchMapper.ValidateBatchAsync(dataTable);
                    
                    // Process validation results
                    foreach (var (rowNum, result) in validationResults)
                    {
                        if (!result.IsValid)
                        {
                            foreach (var error in result.Errors)
                            {
                                validationErrors.Add(new BulkUploadErrorDto
                                {
                                    RowNumber = rowNum,
                                    ErrorMessage = error,
                                    ErrorCategory = error.Contains("(DUPLICATE)") ? ErrorCategoryEnum.DuplicateRecord : ErrorCategoryEnum.ValidationError
                                });
                            }
                            if (!ignoreErrors)
                            {
                                response.FailedRecords++;
                            }
                        }
                    }
                    
                    _logger.LogInformation("Batch validation completed. Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                        validationResults.Count(vr => vr.Value.IsValid), validationResults.Count(vr => !vr.Value.IsValid));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch validation failed, falling back to row-by-row validation");
                    // Fall back to individual validation if batch fails
                    validationResults.Clear();
                    validationErrors.Clear();
                    response.FailedRecords = 0;
                }
            }

            // Fall back to optimized parallel validation if batch not supported or failed
            if (validationResults.Count == 0)
            {
                _logger.LogInformation("Using optimized parallel validation for {TotalRecords} records", totalRecords);
                
                // Process validation in parallel chunks for better performance
                const int chunkSize = 1000;
                var chunks = dataTable.Rows.Cast<DataRow>()
                    .Select((row, index) => new { Row = row, Index = index + 1 })
                    .Chunk(chunkSize)
                    .ToList();
                
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4),
                    CancellationToken = cancellationToken
                };

                var chunkResults = new ConcurrentDictionary<int, ValidationResultDto>();
                var chunkErrors = new ConcurrentBag<BulkUploadErrorDto>();
                var failedCount = 0;

                await Parallel.ForEachAsync(chunks, parallelOptions, async (chunk, ct) =>
                {
                    try
                    {
                        foreach (var item in chunk)
                        {
                            ct.ThrowIfCancellationRequested();
                            
                            try
                            {
                                var validationResult = await mapper.ValidateRowAsync(item.Row, item.Index, ct);
                                chunkResults[item.Index] = validationResult;
                                
                                if (!validationResult.IsValid)
                                {
                                    foreach (var error in validationResult.Errors)
                                    {
                                        chunkErrors.Add(new BulkUploadErrorDto
                                        {
                                            RowNumber = item.Index,
                                            ErrorMessage = error,
                                            RowData = GetRowData(item.Row),
                                            ErrorCategory = error.Contains("(DUPLICATE)") ? ErrorCategoryEnum.DuplicateRecord : ErrorCategoryEnum.ValidationError
                                        });
                                    }
                                    if (!ignoreErrors)
                                    {
                                        Interlocked.Increment(ref failedCount);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                chunkResults[item.Index] = new ValidationResultDto { IsValid = false, Errors = new List<string> { $"Validation error - {ex.Message}" } };
                                chunkErrors.Add(new BulkUploadErrorDto
                                {
                                    RowNumber = item.Index,
                                    ErrorMessage = $"Validation error - {ex.Message}",
                                    RowData = GetRowData(item.Row),
                                    ErrorCategory = ErrorCategoryEnum.SystemError
                                });
                                _logger.LogError(ex, "Validation error for row {RowNumber}", item.Index);
                                if (!ignoreErrors)
                                {
                                    Interlocked.Increment(ref failedCount);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing validation chunk");
                        throw;
                    }
                });

                validationResults = chunkResults.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                validationErrors.AddRange(chunkErrors.OrderBy(e => e.RowNumber));
                response.FailedRecords = failedCount;
                
                _logger.LogInformation("Parallel validation completed for {TotalRecords} records in {ChunkCount} chunks. Failed: {FailedCount}", 
                    totalRecords, chunks.Count, failedCount);
            }

            response.Errors = validationErrors;

            // Log validation errors but continue processing - let mappers skip invalid records
            if (response.FailedRecords > 0)
            {
                _logger.LogWarning("Found {FailedRecords} validation errors. Processing will continue and skip invalid records.", response.FailedRecords);
                
                // If ignoreErrors is false, we still continue but the mappers will skip invalid records
                // Only fail if ALL records are invalid and ignoreErrors is false
                if (!ignoreErrors && response.FailedRecords == response.TotalRecords)
                {
                    response.ProcessingTime = stopwatch.Elapsed;
                    return Result<BulkUploadResponseDto>.Failure($"All records failed validation: {string.Join("; ", validationErrors.Take(5).Select(e => e.ErrorMessage))}");
                }
            }

            // Phase 2: Data Processing - Use high-performance bulk processing
            _logger.LogInformation("Starting high-performance bulk processing for {TotalRecords} rows", totalRecords);
            
            var successfullyProcessed = 0;
            
            // Try SQL Server bulk insert first for maximum performance
            if (_context.Database.IsSqlServer() && mapper is IBatchTableMapper processingBatchMapper)
            {
                _logger.LogInformation("Using SQL Server bulk insert optimization for {TotalRecords} records", totalRecords);
                
                try
                {
                    successfullyProcessed = await ProcessWithBulkInsertAsync(dataTable, mapper, userId, validationResults, ignoreErrors, response, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bulk insert failed, falling back to batch processing");
                    // Fall back to standard batch processing
                    try
                    {
                        var batchResult = await processingBatchMapper.SaveBatchAsync(dataTable, userId, validationResults, ignoreErrors, cancellationToken);
                        if (batchResult.IsSuccess)
                        {
                            successfullyProcessed = batchResult.Data;
                            await _context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("Batch processing completed. Processed {ProcessedCount} records", successfullyProcessed);
                        }
                        else
                        {
                            _logger.LogError("Batch processing failed: {ErrorMessage}", batchResult.ErrorMessage);
                            throw new Exception($"Batch processing failed: {batchResult.ErrorMessage}");
                        }
                    }
                    catch (Exception batchEx)
                    {
                        _logger.LogError(batchEx, "Batch processing also failed, falling back to optimized row processing");
                        successfullyProcessed = await ProcessRowByRowOptimizedAsync(dataTable, mapper, userId, validationResults, ignoreErrors, response, cancellationToken);
                    }
                }
            }
            else if (mapper is IBatchTableMapper standardBatchMapper)
            {
                _logger.LogInformation("Using optimized batch processing for {TotalRecords} records", totalRecords);
                
                try
                {
                    var batchResult = await standardBatchMapper.SaveBatchAsync(dataTable, userId, validationResults, ignoreErrors, cancellationToken);
                    if (batchResult.IsSuccess)
                    {
                        successfullyProcessed = batchResult.Data;
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Batch processing completed. Processed {ProcessedCount} records", successfullyProcessed);
                    }
                    else
                    {
                        _logger.LogError("Batch processing failed: {ErrorMessage}", batchResult.ErrorMessage);
                        throw new Exception($"Batch processing failed: {batchResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch processing failed, falling back to optimized row processing");
                    successfullyProcessed = await ProcessRowByRowOptimizedAsync(dataTable, mapper, userId, validationResults, ignoreErrors, response, cancellationToken);
                }
            }
            else
            {
                // Fall back to optimized individual processing with parallel execution
                _logger.LogInformation("Using optimized parallel row processing for {TotalRecords} records", totalRecords);
                successfullyProcessed = await ProcessRowByRowOptimizedAsync(dataTable, mapper, userId, validationResults, ignoreErrors, response, cancellationToken);
            }

            response.ProcessedRecords = successfullyProcessed;
            
            stopwatch.Stop();
            response.ProcessingTime = stopwatch.Elapsed;
            
            await LogBulkUploadHistory(userId, tableType, response);
            
            _logger.LogInformation("Successfully completed batch processing. Total processed: {ProcessedRecords}", successfullyProcessed);

            return Result<BulkUploadResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk upload");
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
                .Where(h => h.UserId == userId);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(h => h.UploadedAt)
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

        // Optimize for large Excel files - streaming approach with memory management
        var rowCount = worksheet.Dimension.End.Row;
        var colCount = worksheet.Dimension.End.Column;
        
        _logger.LogInformation("Processing Excel file with {RowCount} rows and {ColCount} columns", rowCount, colCount);

        // Add columns with proper data types for better performance
        for (int col = 1; col <= colCount; col++)
        {
            var columnName = worksheet.Cells[1, col].Value?.ToString() ?? $"Column{col}";
            dataTable.Columns.Add(columnName, typeof(string));
        }

        // Memory-optimized batch processing with dynamic sizing
        var memoryOptimizedBatchSize = CalculateMemoryOptimizedBatchSize(rowCount, colCount);
        
        for (int startRow = 2; startRow <= rowCount; startRow += memoryOptimizedBatchSize)
        {
            var endRow = Math.Min(startRow + memoryOptimizedBatchSize - 1, rowCount);
            
            // Process batch with minimal memory allocation
            var batchRows = new List<object[]>(endRow - startRow + 1);
            
            for (int row = startRow; row <= endRow; row++)
            {
                var rowData = new object[colCount];
                for (int col = 1; col <= colCount; col++)
                {
                    var cellValue = worksheet.Cells[row, col].Value;
                    rowData[col - 1] = cellValue?.ToString() ?? string.Empty;
                }
                batchRows.Add(rowData);
            }
            
            // Add batch to DataTable efficiently
            foreach (var rowData in batchRows)
            {
                var dataRow = dataTable.NewRow();
                dataRow.ItemArray = rowData;
                dataTable.Rows.Add(dataRow);
            }
            
            // Force garbage collection for large files to manage memory
            if (rowCount > 50000 && startRow % (memoryOptimizedBatchSize * 5) == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                _logger.LogDebug("Memory cleanup performed at row {CurrentRow}/{TotalRows}", endRow - 1, rowCount - 1);
            }
            
            // Progress logging for large files
            if (rowCount > 5000 && startRow % (memoryOptimizedBatchSize * 2) == 0)
            {
                _logger.LogDebug("Parsed {ProcessedRows}/{TotalRows} Excel rows", Math.Min(endRow - 1, rowCount - 1), rowCount - 1);
            }
        }

        _logger.LogInformation("Successfully parsed Excel file: {RowCount} data rows, {ColCount} columns", rowCount - 1, colCount);
        return dataTable;
    }

    private int CalculateMemoryOptimizedBatchSize(int rowCount, int colCount)
    {
        // Calculate optimal batch size based on memory constraints and data size
        var baseBatchSize = rowCount switch
        {
            < 1000 => 500,
            < 5000 => 1000,
            < 20000 => 2000,
            < 100000 => 5000,
            _ => 10000
        };
        
        // Adjust for column count (more columns = smaller batches)
        var columnAdjustment = colCount switch
        {
            < 10 => 1.0,
            < 25 => 0.8,
            < 50 => 0.6,
            < 100 => 0.4,
            _ => 0.2
        };
        
        return Math.Max(100, (int)(baseBatchSize * columnAdjustment));
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

            var arrayLength = jsonDoc.RootElement.GetArrayLength();
            _logger.LogInformation("Processing JSON file with {RecordCount} records", arrayLength);

            // Get columns from first object
            var firstElement = jsonDoc.RootElement[0];
            if (firstElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                // Add columns based on first object properties with proper types
                foreach (var property in firstElement.EnumerateObject())
                {
                    dataTable.Columns.Add(property.Name, typeof(string));
                }

                // Add rows for all objects in array with progress tracking
                var processedCount = 0;
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
                        processedCount++;
                        
                        // Progress logging for large files
                        if (arrayLength > 5000 && processedCount % 5000 == 0)
                        {
                            _logger.LogDebug("Parsed {ProcessedRecords}/{TotalRecords} JSON records", processedCount, arrayLength);
                        }
                    }
                }
                
                _logger.LogInformation("Successfully parsed JSON file: {RecordCount} records, {ColumnCount} columns", 
                    dataTable.Rows.Count, dataTable.Columns.Count);
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

    private async Task LogBulkUploadHistory(Guid userId, string tableType, BulkUploadResponseDto responseDto)
    {
        var history = new BulkUploadHistoryModel
        {
            UploadId = Guid.NewGuid(),
            UserId = userId,
            TableType = tableType,
            FileName = responseDto.FileName ?? "BulkUpload", // Use actual filename from response
            TotalRecords = responseDto.TotalRecords,
            ProcessedRecords = responseDto.ProcessedRecords,
            FailedRecords = responseDto.FailedRecords,
            UploadedAt = responseDto.ProcessedAt,
            Status = responseDto.FailedRecords == 0 ? "Success" : responseDto.ProcessedRecords == 0 ? "Failed" : "Partial",
            ProcessingTime = responseDto.ProcessingTime,
            ErrorDetails = responseDto.Errors.Any() ? System.Text.Json.JsonSerializer.Serialize(responseDto.Errors) : null
        };

        _context.BulkUploadHistories.Add(history);
        _logger.LogInformation("Created BulkUploadHistory record for user {UserId}, table {TableType}, file {FileName}", 
            userId, tableType, history.FileName);
        // Note: SaveChanges will be called by the controller's transaction
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
                return Result<BulkUploadResponseDto>.Failure($"No mapper found for table type: {tableType}");
            }

            _logger.LogInformation("Found mapper for table type: {TableType}", tableType);

            var response = new BulkUploadResponseDto
            {
                TotalRecords = totalRecords,
                ProcessedRecords = 0,
                FailedRecords = 0,
                TableType = tableType,
                ProcessedAt = DateTime.UtcNow,
                Errors = new List<BulkUploadErrorDto>(),
                FileName = fileName
            };

            // Progress tracking variables
            var processedRecords = 0;
            var validationErrors = new List<BulkUploadErrorDto>();
            var validationResults = new Dictionary<int, ValidationResultDto>();
            
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

            // Use batch validation if supported for better performance
            if (mapper is IBatchTableMapper progressBatchMapper)
            {
                _logger.LogInformation("Using optimized batch validation for {TotalRecords} records", totalRecords);
                
                try
                {
                    validationResults = await progressBatchMapper.ValidateBatchAsync(dataTable, cancellationToken);
                    
                    // Process validation results
                    foreach (var (rowNum, result) in validationResults)
                    {
                        if (!result.IsValid)
                        {
                            foreach (var error in result.Errors)
                            {
                                validationErrors.Add(new BulkUploadErrorDto
                                {
                                    RowNumber = rowNum,
                                    ErrorMessage = error,
                                    FileName = fileName,
                                    ErrorCategory = error.Contains("(DUPLICATE)") ? ErrorCategoryEnum.DuplicateRecord : ErrorCategoryEnum.ValidationError
                                });
                            }
                            if (!ignoreErrors)
                            {
                                response.FailedRecords++;
                            }
                        }
                    }
                    
                    processedRecords = totalRecords; // All records processed in batch
                    _logger.LogInformation("Batch validation completed. Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                        validationResults.Count(vr => vr.Value.IsValid), validationResults.Count(vr => !vr.Value.IsValid));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch validation failed, falling back to row-by-row validation");
                    // Fall back to individual validation if batch fails
                    validationResults.Clear();
                    validationErrors.Clear();
                    response.FailedRecords = 0;
                    processedRecords = 0;
                }
            }

            // Fall back to row-by-row validation if batch not supported or failed
            if (processedRecords == 0)
            {
                _logger.LogInformation("Using row-by-row validation for {TotalRecords} records", totalRecords);
                
                var fallbackRowNumber = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    // Check for cancellation before each validation
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        var validationResult = await mapper.ValidateRowAsync(row, fallbackRowNumber, cancellationToken);
                        validationResults[fallbackRowNumber] = validationResult;
                        
                        if (!validationResult.IsValid)
                        {
                            foreach (var error in validationResult.Errors)
                            {
                                validationErrors.Add(new BulkUploadErrorDto
                                {
                                    RowNumber = fallbackRowNumber,
                                    ErrorMessage = error,
                                    FileName = fileName,
                                    ErrorCategory = error.Contains("(DUPLICATE)") ? ErrorCategoryEnum.DuplicateRecord : ErrorCategoryEnum.ValidationError
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
                        validationResults[fallbackRowNumber] = new ValidationResultDto { IsValid = false, Errors = new List<string> { $"Validation error - {ex.Message}" } };
                        validationErrors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = fallbackRowNumber,
                            ErrorMessage = $"Validation error - {ex.Message}",
                            FileName = fileName,
                            ErrorCategory = ErrorCategoryEnum.SystemError
                        });
                        _logger.LogError(ex, "Validation error for row {RowNumber}", fallbackRowNumber);
                        if (!ignoreErrors)
                        {
                            response.FailedRecords++;
                        }
                    }

                    fallbackRowNumber++;
                    processedRecords++;
                    
                    if (processedRecords % Math.Max(1, totalRecords / 10) == 0)
                    {
                        var validationProgress = (double)processedRecords / totalRecords * 30;
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
                            Errors = validationErrors.Count > 0 ? validationErrors.TakeLast(3).Select(e => e.ErrorMessage).ToList() : null
                        });
                    }
                }
            }

            response.Errors = validationErrors;

            // Log validation errors but continue processing - let mappers skip invalid records
            if (response.FailedRecords > 0)
            {
                _logger.LogWarning("Found {FailedRecords} validation errors. Processing will continue and skip invalid records.", response.FailedRecords);
                
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (40.0 / totalFiles),
                    Status = "Validation Completed",
                    CurrentOperation = $"Found {response.FailedRecords} validation errors - will skip invalid records and continue",
                    ProcessedRecords = processedRecords,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles,
                    Errors = validationErrors.TakeLast(5).Select(e => e.ErrorMessage).ToList()
                });

                // Only fail if ALL records are invalid and ignoreErrors is false
                if (!ignoreErrors && response.FailedRecords == response.TotalRecords)
                {
                    response.ProcessingTime = stopwatch.Elapsed;
                    return Result<BulkUploadResponseDto>.Failure($"All records failed validation: {string.Join("; ", validationErrors.Take(5).Select(e => e.ErrorMessage))}");
                }
            }
            
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
            
            var successfullyProcessed = 0;
            var rowNumber = 1;
            const int batchSize = 100;
            var totalBatches = (int)Math.Ceiling((double)dataTable.Rows.Count / batchSize);
            
            _logger.LogInformation("Processing {TotalRecords} records in {TotalBatches} batches of {BatchSize}", 
                totalRecords, totalBatches, batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                // Check for cancellation at batch level
                cancellationToken.ThrowIfCancellationRequested();
                
                var batchStart = batchIndex * batchSize;
                var batchEnd = Math.Min(batchStart + batchSize, dataTable.Rows.Count);
                var batchRows = dataTable.Rows.Cast<DataRow>().Skip(batchStart).Take(batchEnd - batchStart).ToList();
                
                _logger.LogDebug("Processing batch {BatchIndex}/{TotalBatches} (rows {BatchStart}-{BatchEnd}) for job {JobId}", 
                    batchIndex + 1, totalBatches, batchStart + 1, batchEnd, jobId);

                // Process batch
                var batchProcessed = 0;
                var currentRowNumber = rowNumber;
                
                foreach (var row in batchRows)
                {
                    try
                    {
                        // Skip rows that failed validation if not ignoring errors
                        var validationResult = validationResults.ContainsKey(currentRowNumber) ? validationResults[currentRowNumber] : new ValidationResultDto { IsValid = false };
                        if (!validationResult.IsValid && !ignoreErrors)
                        {
                            currentRowNumber++;
                            continue;
                        }

                        var saveResult = await mapper.SaveRowAsync(row, userId, cancellationToken);
                        if (saveResult.IsSuccess)
                        {
                            batchProcessed++;
                        }
                        else
                        {
                            response.FailedRecords++;
                            response.Errors.Add(new BulkUploadErrorDto
                            {
                                RowNumber = currentRowNumber,
                                ErrorMessage = saveResult.ErrorMessage,
                                FileName = fileName,
                                ErrorCategory = ErrorCategoryEnum.ProcessingError
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailedRecords++;
                        response.Errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = currentRowNumber,
                            ErrorMessage = $"Processing error - {ex.Message}",
                            FileName = fileName,
                            ErrorCategory = ErrorCategoryEnum.SystemError
                        });
                        _logger.LogError(ex, "Processing error for row {RowNumber}", currentRowNumber);
                    }

                    currentRowNumber++;
                }

                successfullyProcessed += batchProcessed;
                rowNumber = currentRowNumber;

                // Save batch to database (more efficient than individual saves)
                if (batchProcessed > 0)
                {
                    try
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Saved batch {BatchIndex}/{TotalBatches} - {BatchProcessed} records", 
                                batchIndex + 1, totalBatches, batchProcessed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving batch {BatchIndex}", batchIndex + 1);
                        // Could implement retry logic here
                        throw;
                    }
                }

                // Update progress every batch (much less frequent than per row)
                var processingProgress = (double)successfullyProcessed / totalRecords * 60; // 60% for processing
                await _progressService.NotifyProgress(jobId, new ProgressUpdateDto
                {
                    JobId = jobId,
                    ProgressPercentage = baseProgress + (40.0 / totalFiles) + (processingProgress / totalFiles),
                    Status = "Processing",
                    CurrentOperation = $"Processed {successfullyProcessed}/{totalRecords} records (Batch {batchIndex + 1}/{totalBatches})",
                    ProcessedRecords = successfullyProcessed,
                    TotalRecords = totalRecords,
                    CurrentFileName = fileName,
                    ProcessedFiles = fileIndex,
                    TotalFiles = totalFiles,
                    Errors = response.Errors.Count > 0 ? response.Errors.TakeLast(3).Select(e => e.ErrorMessage).ToList() : null
                });
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

            // Check for cancellation before final save
            cancellationToken.ThrowIfCancellationRequested();
            
            // All batches already saved - no need for final SaveChanges
            _logger.LogInformation("Successfully completed batch processing for job {JobId}. Total processed: {ProcessedRecords}", 
                jobId, successfullyProcessed);

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

            // Notify job completion
            await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
            {
                JobId = jobId,
                Success = true,
                Message = "Migration completed successfully",
                Data = response,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = stopwatch.Elapsed
            });

            return Result<BulkUploadResponseDto>.Success(response);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("⚠️ ProcessBulkDataWithProgressAsync was CANCELLED for job {JobId} - cancellation detected", jobId);
            
            await _progressService.NotifyError(jobId, "Migration cancelled by user");
            
            // Notify job completion with cancellation
            await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
            {
                JobId = jobId,
                Success = false,
                Message = "Migration cancelled by user",
                Data = null,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = stopwatch.Elapsed
            });
            
            return Result<BulkUploadResponseDto>.Failure("Migration was cancelled by user");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in ProcessBulkDataWithProgressAsync for job {JobId}", jobId);
            
            await _progressService.NotifyError(jobId, $"Processing error: {ex.Message}");
            
            // Notify job completion with error
            await _progressService.NotifyJobCompleted(jobId, new JobCompleteDto
            {
                JobId = jobId,
                Success = false,
                Message = $"Migration failed: {ex.Message}",
                Data = null,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = stopwatch.Elapsed
            });
            
            return Result<BulkUploadResponseDto>.Failure($"Error processing bulk data: {ex.Message}");
        }
    }

    private async Task<int> ProcessRowByRowAsync(DataTable dataTable, ITableMapper mapper, Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors, BulkUploadResponseDto responseDto, 
        CancellationToken cancellationToken)
    {
        var successfullyProcessed = 0;
        var rowNumber = 1;
        
        // Dynamic batch sizing based on table size - optimized for SQL Server bulk operations
        var dynamicBatchSize = dataTable.Rows.Count switch
        {
            < 100 => 50,
            < 1000 => 200,
            < 10000 => 500,
            < 50000 => 1000,
            _ => 2000
        };
        
        var totalBatches = (int)Math.Ceiling((double)dataTable.Rows.Count / dynamicBatchSize);
        
        _logger.LogInformation("Processing {TotalRecords} records in {TotalBatches} batches of {BatchSize}", 
            dataTable.Rows.Count, totalBatches, dynamicBatchSize);

        // Single transaction for entire operation - major performance boost
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Disable change tracking for bulk operations - significant memory optimization
            var originalChangeTrackingState = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            
            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batchStart = batchIndex * dynamicBatchSize;
                var batchEnd = Math.Min(batchStart + dynamicBatchSize, dataTable.Rows.Count);
                var batchRows = dataTable.Rows.Cast<DataRow>().Skip(batchStart).Take(batchEnd - batchStart).ToList();
                
                _logger.LogDebug("Processing batch {BatchIndex}/{TotalBatches} (rows {BatchStart}-{BatchEnd})", 
                    batchIndex + 1, totalBatches, batchStart + 1, batchEnd);

                var batchProcessed = 0;
                var currentRowNumber = rowNumber;
                
                foreach (var row in batchRows)
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Skip rows that failed validation if not ignoring errors
                        var validationResult = validationResults.ContainsKey(currentRowNumber) ? 
                            validationResults[currentRowNumber] : new ValidationResultDto { IsValid = false };
                        if (!validationResult.IsValid && !ignoreErrors)
                        {
                            currentRowNumber++;
                            continue;
                        }

                        var saveResult = await mapper.SaveRowAsync(row, userId, cancellationToken);
                        if (saveResult.IsSuccess)
                        {
                            batchProcessed++;
                        }
                        else
                        {
                            responseDto.FailedRecords++;
                            responseDto.Errors.Add(new BulkUploadErrorDto
                            {
                                RowNumber = currentRowNumber,
                                ErrorMessage = saveResult.ErrorMessage ?? "Unknown error",
                                RowData = GetRowData(row),
                                ErrorCategory = ErrorCategoryEnum.ProcessingError
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        responseDto.FailedRecords++;
                        responseDto.Errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = currentRowNumber,
                            ErrorMessage = $"Processing error - {ex.Message}",
                            RowData = GetRowData(row),
                            ErrorCategory = ErrorCategoryEnum.SystemError
                        });
                        _logger.LogError(ex, "Processing error for row {RowNumber}", currentRowNumber);
                    }

                    currentRowNumber++;
                }

                successfullyProcessed += batchProcessed;
                rowNumber = currentRowNumber;
                
                // Progress reporting every 10 batches to reduce log noise
                if (batchIndex % 10 == 0 || batchIndex == totalBatches - 1)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                        _logger.LogDebug("Processed batch {BatchIndex}/{TotalBatches} - {BatchProcessed} records total: {TotalProcessed}", 
                            batchIndex + 1, totalBatches, batchProcessed, successfullyProcessed);
                }
            }
            
            // Single SaveChanges call for all batches - massive performance improvement
            if (successfullyProcessed > 0)
            {
                _logger.LogInformation("Saving {TotalRecords} records in single transaction", successfullyProcessed);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully saved all {TotalRecords} records", successfullyProcessed);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            
            // Restore change tracking
            _context.ChangeTracker.AutoDetectChangesEnabled = originalChangeTrackingState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch processing, rolling back transaction");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        
        return successfullyProcessed;
    }

    private async Task<int> ProcessWithBulkInsertAsync(DataTable dataTable, ITableMapper mapper, Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors, BulkUploadResponseDto responseDto, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SQL Server bulk insert optimization for {TotalRecords} records", dataTable.Rows.Count);
        
        // Filter valid rows and create optimized DataTable for bulk insert
        var validDataTable = CreateFilteredDataTable(dataTable, validationResults, ignoreErrors);
        
        if (validDataTable.Rows.Count == 0)
        {
            _logger.LogWarning("No valid records found for bulk insert");
            return 0;
        }

        _logger.LogInformation("Filtered to {ValidRecords} valid records for bulk insert", validDataTable.Rows.Count);

        // Use bulk insert service if available
        var bulkInsertService = new SqlServerBulkInsertService(_context, 
            _logger as ILogger<SqlServerBulkInsertService> ?? 
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SqlServerBulkInsertService>());
        
        if (bulkInsertService.IsBulkOperationSupported)
        {
            var tableName = GetTableNameForMapper(mapper);
            var columnMappings = GetColumnMappingsForMapper(mapper, validDataTable);
            
            _logger.LogInformation("Performing bulk insert to table {TableName}", tableName);
            
            var result = await bulkInsertService.BulkInsertAsync(validDataTable, tableName, columnMappings, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Bulk insert completed successfully. Inserted {RecordCount} records", result.Data);
                return result.Data;
            }
            else
            {
                _logger.LogError("Bulk insert failed: {ErrorMessage}", result.ErrorMessage);
                throw new Exception($"Bulk insert failed: {result.ErrorMessage}");
            }
        }
        
        throw new Exception("Bulk insert not supported for current database provider");
    }

    private async Task<int> ProcessRowByRowOptimizedAsync(DataTable dataTable, ITableMapper mapper, Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors, BulkUploadResponseDto responseDto, 
        CancellationToken cancellationToken)
    {
        var totalRecords = dataTable.Rows.Count;
        _logger.LogInformation("Starting optimized parallel row processing for {TotalRecords} records", totalRecords);
        
        // Optimized batch sizing based on record count and system resources
        var optimalBatchSize = CalculateOptimalBatchSize(totalRecords);
        var totalBatches = (int)Math.Ceiling((double)totalRecords / optimalBatchSize);
        
        _logger.LogInformation("Processing {TotalRecords} records in {TotalBatches} optimized batches of {BatchSize}", 
            totalRecords, totalBatches, optimalBatchSize);

        var successfullyProcessed = 0;
        var processingOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount / 2, 3), // Leave some CPU for other operations
            CancellationToken = cancellationToken
        };

        // Process in optimized batches with parallel execution within reasonable limits
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Disable change tracking for massive performance boost
            var originalChangeTrackingState = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            
            var batches = Enumerable.Range(0, totalBatches)
                .Select(batchIndex => new 
                {
                    BatchIndex = batchIndex,
                    StartIndex = batchIndex * optimalBatchSize,
                    EndIndex = Math.Min((batchIndex + 1) * optimalBatchSize, totalRecords)
                })
                .ToList();

            var processedCounts = new ConcurrentBag<int>();
            var errors = new ConcurrentBag<BulkUploadErrorDto>();

            await Parallel.ForEachAsync(batches, processingOptions, async (batch, ct) =>
            {
                var batchProcessed = 0;
                
                try
                {
                    var batchRows = dataTable.Rows.Cast<DataRow>()
                        .Skip(batch.StartIndex)
                        .Take(batch.EndIndex - batch.StartIndex)
                        .ToList();

                    var rowNumber = batch.StartIndex + 1;
                    
                    foreach (var row in batchRows)
                    {
                        ct.ThrowIfCancellationRequested();
                        
                        try
                        {
                            // Skip rows that failed validation if not ignoring errors
                            var validationResult = validationResults.ContainsKey(rowNumber) ? 
                                validationResults[rowNumber] : new ValidationResultDto { IsValid = false };
                            if (!validationResult.IsValid && !ignoreErrors)
                            {
                                rowNumber++;
                                continue;
                            }

                            var saveResult = await mapper.SaveRowAsync(row, userId, ct);
                            if (saveResult.IsSuccess)
                            {
                                batchProcessed++;
                            }
                            else
                            {
                                errors.Add(new BulkUploadErrorDto
                                {
                                    RowNumber = rowNumber,
                                    ErrorMessage = saveResult.ErrorMessage ?? "Unknown error",
                                    RowData = GetRowData(row),
                                    ErrorCategory = ErrorCategoryEnum.ProcessingError
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new BulkUploadErrorDto
                            {
                                RowNumber = rowNumber,
                                ErrorMessage = $"Processing error - {ex.Message}",
                                RowData = GetRowData(row),
                                ErrorCategory = ErrorCategoryEnum.SystemError
                            });
                            _logger.LogError(ex, "Processing error for row {RowNumber} in batch {BatchIndex}", rowNumber, batch.BatchIndex);
                        }

                        rowNumber++;
                    }
                    
                    processedCounts.Add(batchProcessed);
                    
                    _logger.LogDebug("Batch {BatchIndex}/{TotalBatches} completed - {BatchProcessed} records processed", 
                        batch.BatchIndex + 1, totalBatches, batchProcessed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing batch {BatchIndex}", batch.BatchIndex);
                    throw;
                }
            });

            successfullyProcessed = processedCounts.Sum();
            responseDto.Errors.AddRange(errors.OrderBy(e => e.RowNumber));
            responseDto.FailedRecords += errors.Count;
            
            // Single SaveChanges call for all batches - massive performance improvement
            if (successfullyProcessed > 0)
            {
                _logger.LogInformation("Saving {TotalRecords} records in single transaction", successfullyProcessed);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully saved all {TotalRecords} records", successfullyProcessed);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            
            // Restore change tracking
            _context.ChangeTracker.AutoDetectChangesEnabled = originalChangeTrackingState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in optimized parallel processing, rolling back transaction");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        
        _logger.LogInformation("Optimized parallel processing completed. Processed: {ProcessedRecords}, Failed: {FailedRecords}", 
            successfullyProcessed, responseDto.FailedRecords);
        
        return successfullyProcessed;
    }

    private DataTable CreateFilteredDataTable(DataTable originalTable, Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors)
    {
        var filteredTable = originalTable.Clone(); // Copy structure
        
        var rowNumber = 1;
        foreach (DataRow row in originalTable.Rows)
        {
            var validationResult = validationResults.ContainsKey(rowNumber) ? 
                validationResults[rowNumber] : new ValidationResultDto { IsValid = false };
                
            if (validationResult.IsValid || ignoreErrors)
            {
                var newRow = filteredTable.NewRow();
                newRow.ItemArray = row.ItemArray;
                filteredTable.Rows.Add(newRow);
            }
            
            rowNumber++;
        }
        
        return filteredTable;
    }

    private int CalculateOptimalBatchSize(int totalRecords)
    {
        // Dynamic batch sizing based on record count and system capabilities
        return totalRecords switch
        {
            < 1000 => 200,
            < 5000 => 500,
            < 20000 => 1000,
            < 100000 => 2000,
            _ => 5000
        };
    }

    private string GetTableNameForMapper(ITableMapper mapper)
    {
        // Map table types to actual database table names
        return mapper.TableType switch
        {
            "TemporaryUsers" => "TemporaryUsers",
            "UserRoles" => "UserRoles",
            "Users" => "Users",
            _ => throw new NotSupportedException($"Table type {mapper.TableType} not supported for bulk insert")
        };
    }

    private Dictionary<string, string> GetColumnMappingsForMapper(ITableMapper mapper, DataTable dataTable)
    {
        // Create column mappings from DataTable columns to database columns
        var mappings = new Dictionary<string, string>();
        
        foreach (DataColumn column in dataTable.Columns)
        {
            mappings[column.ColumnName] = column.ColumnName; // Assuming same names
        }
        
        return mappings;
    }

}