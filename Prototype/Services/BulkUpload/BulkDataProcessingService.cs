using System.Data;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Helpers;
using Prototype.Models;
using Prototype.Services.Interfaces;

namespace Prototype.Services.BulkUpload;

public class BulkDataProcessingService : IBulkDataProcessingService
{
    private readonly SentinelContext _context;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<BulkDataProcessingService> _logger;
    private readonly ITableMappingService _tableMappingService;
    private readonly IProgressService _progressService;
    private readonly IBulkValidationService _validationService;

    public BulkDataProcessingService(
        SentinelContext context,
        ITransactionService transactionService,
        ILogger<BulkDataProcessingService> logger,
        ITableMappingService tableMappingService,
        IProgressService progressService,
        IBulkValidationService validationService)
    {
        _context = context;
        _transactionService = transactionService;
        _logger = logger;
        _tableMappingService = tableMappingService;
        _progressService = progressService;
        _validationService = validationService;
    }

    public async Task<Result<BulkUploadResponseDto>> ProcessDataAsync(
        DataTable dataTable, 
        string tableType, 
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ProcessDataAsync for table: {TableType}, userId: {UserId}", tableType, userId);
        
        var stopwatch = Stopwatch.StartNew();
        var response = new BulkUploadResponseDto
        {
            TableType = tableType,
            ProcessedAt = DateTime.UtcNow,
            Errors = new List<BulkUploadErrorDto>()
        };

        try
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                _logger.LogWarning("No data found in data table");
                return Result<BulkUploadResponseDto>.Failure("No data found to process");
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

            // Phase 1: Validation
            var validationResults = await _validationService.ValidateWithResultsAsync(dataTable, tableType, cancellationToken);
            var validationErrors = new List<BulkUploadErrorDto>();
            
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

            response.Errors.AddRange(validationErrors);

            if (validationErrors.Any() && !ignoreErrors)
            {
                _logger.LogWarning("Validation failed with {ErrorCount} errors, aborting processing", validationErrors.Count);
                return Result<BulkUploadResponseDto>.Failure($"Validation failed with {validationErrors.Count} errors");
            }

            // Phase 2: Processing
            var successfullyProcessed = await ProcessValidRecordsAsync(dataTable, mapper, userId, validationResults, ignoreErrors, response, cancellationToken);
            
            response.ProcessedRecords = successfullyProcessed;
            response.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogInformation("Processing completed. Success: {SuccessCount}, Failed: {FailCount}, Duration: {Duration}", 
                response.ProcessedRecords, response.FailedRecords, response.ProcessingTime);

            // Log bulk upload history
            await LogBulkUploadHistory(userId, tableType, response);

            return Result<BulkUploadResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk data");
            return Result<BulkUploadResponseDto>.Failure($"Error processing bulk data: {ex.Message}");
        }
    }

    public async Task<Result<BulkUploadResponseDto>> ProcessDataWithProgressAsync(
        DataTable dataTable, 
        string tableType, 
        Guid userId, 
        bool ignoreErrors = false,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        _logger.LogInformation("Starting ProcessDataWithProgressAsync for table: {TableType}, userId: {UserId}, jobId: {JobId}", tableType, userId, jobId);
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Notify job started
            await _progressService.NotifyJobStarted(jobId.ToString(), new JobStartDto
            {
                JobId = jobId.ToString(),
                JobType = tableType,
                TotalFiles = 1,
                EstimatedTotalRecords = dataTable?.Rows.Count ?? 0,
                StartTime = DateTime.UtcNow
            });

            // Process the data
            var result = await ProcessDataAsync(dataTable, tableType, userId, ignoreErrors, cancellationToken);
            
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                // Notify job completed successfully
                await _progressService.NotifyJobCompleted(jobId.ToString(), new JobCompleteDto
                {
                    JobId = jobId.ToString(),
                    Success = true,
                    Message = "Processing completed successfully",
                    Data = result.Data,
                    CompletedAt = DateTime.UtcNow,
                    TotalDuration = stopwatch.Elapsed
                });
            }
            else
            {
                // Notify job completed with error
                await _progressService.NotifyJobCompleted(jobId.ToString(), new JobCompleteDto
                {
                    JobId = jobId.ToString(),
                    Success = false,
                    Message = result.ErrorMessage,
                    Data = null,
                    CompletedAt = DateTime.UtcNow,
                    TotalDuration = stopwatch.Elapsed
                });
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("ProcessDataWithProgressAsync was cancelled for job {JobId}", jobId);
            
            await _progressService.NotifyError(jobId.ToString(), "Processing cancelled by user");
            
            await _progressService.NotifyJobCompleted(jobId.ToString(), new JobCompleteDto
            {
                JobId = jobId.ToString(),
                Success = false,
                Message = "Processing cancelled by user",
                Data = null,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = stopwatch.Elapsed
            });
            
            return Result<BulkUploadResponseDto>.Failure("Processing was cancelled by user");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in ProcessDataWithProgressAsync for job {JobId}", jobId);
            
            await _progressService.NotifyError(jobId.ToString(), $"Processing error: {ex.Message}");
            
            await _progressService.NotifyJobCompleted(jobId.ToString(), new JobCompleteDto
            {
                JobId = jobId.ToString(),
                Success = false,
                Message = $"Processing failed: {ex.Message}",
                Data = null,
                CompletedAt = DateTime.UtcNow,
                TotalDuration = stopwatch.Elapsed
            });
            
            return Result<BulkUploadResponseDto>.Failure($"Error processing bulk data: {ex.Message}");
        }
    }

    private async Task<int> ProcessValidRecordsAsync(
        DataTable dataTable, 
        ITableMapper mapper, 
        Guid userId, 
        Dictionary<int, ValidationResultDto> validationResults, 
        bool ignoreErrors, 
        BulkUploadResponseDto responseDto, 
        CancellationToken cancellationToken)
    {
        var successfullyProcessed = 0;
        var rowNumber = 1;
        
        // Dynamic batch sizing based on table size
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

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
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
                        
                        // Skip invalid rows if ignoreErrors is true
                        if (validationResults.ContainsKey(currentRowNumber) && 
                            !validationResults[currentRowNumber].IsValid && ignoreErrors)
                        {
                            currentRowNumber++;
                            continue;
                        }

                        var saveResult = await mapper.SaveRowAsync(row, userId, cancellationToken);
                        if (saveResult.IsSuccess)
                        {
                            batchProcessed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing row {RowNumber}", currentRowNumber);
                        responseDto.Errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = currentRowNumber,
                            ErrorMessage = ex.Message,
                            ErrorCategory = ErrorCategoryEnum.ProcessingError
                        });
                        
                        if (!ignoreErrors)
                        {
                            responseDto.FailedRecords++;
                        }
                    }
                    
                    currentRowNumber++;
                }
                
                // Update totals
                if (batchProcessed > 0)
                {
                    successfullyProcessed += batchProcessed;
                    
                    _logger.LogDebug("Batch {BatchIndex} completed. Processed: {BatchProcessed}", 
                        batchIndex + 1, batchProcessed);
                }
                
                rowNumber = currentRowNumber;
            }
            
            _context.ChangeTracker.AutoDetectChangesEnabled = originalChangeTrackingState;
            await transaction.CommitAsync(cancellationToken);
            
            _logger.LogInformation("Successfully processed {SuccessfullyProcessed} records", successfullyProcessed);
            return successfullyProcessed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch processing, rolling back transaction");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task LogBulkUploadHistory(Guid userId, string tableType, BulkUploadResponseDto responseDto)
    {
        try
        {
            var history = new BulkUploadHistoryModel
            {
                UploadId = Guid.NewGuid(),
                UserId = userId,
                TableType = tableType,
                FileName = "bulk_upload",
                TotalRecords = responseDto.TotalRecords,
                ProcessedRecords = responseDto.ProcessedRecords,
                FailedRecords = responseDto.FailedRecords,
                UploadedAt = responseDto.ProcessedAt,
                ProcessingTime = responseDto.ProcessingTime,
                Status = responseDto.FailedRecords > 0 ? "Completed with errors" : "Completed"
            };

            await _context.BulkUploadHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log bulk upload history");
        }
    }
}