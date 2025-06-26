using System.Data;
using Microsoft.Extensions.Logging;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;

namespace Prototype.Services.BulkUpload;

public class BulkValidationService(
    ITableMappingService tableMappingService,
    ILogger<BulkValidationService> logger)
    : IBulkValidationService
{
    public async Task<Result<bool>> ValidateDataAsync(DataTable dataTable, string tableType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                return Result<bool>.Failure("No data found to validate");
            }

            var mapper = tableMappingService.GetMapper(tableType);
            if (mapper == null)
            {
                return Result<bool>.Failure($"No mapper found for table type: {tableType}");
            }

            var validationErrors = new List<string>();
            
            logger.LogInformation("Starting validation for {TotalRecords} records", dataTable.Rows.Count);

            // Use batch validation if supported for better performance
            if (mapper is IBatchTableMapper batchMapper)
            {
                logger.LogInformation("Using optimized batch validation for {TotalRecords} records", dataTable.Rows.Count);
                
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
                    
                    logger.LogInformation("Batch validation completed. Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                        validationResults.Count(vr => vr.Value.IsValid), validationResults.Count(vr => !vr.Value.IsValid));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Batch validation failed, falling back to row-by-row validation");
                    // Fall back to individual validation if batch fails
                    validationErrors.Clear();
                }
            }
            
            // Fall back to row-by-row validation if batch not supported or failed
            if (!validationErrors.Any() && !(mapper is IBatchTableMapper))
            {
                logger.LogInformation("Using row-by-row validation for {TotalRecords} records", dataTable.Rows.Count);
                
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
            logger.LogError(ex, "Error validating bulk upload data");
            return Result<bool>.Failure($"Validation error: {ex.Message}");
        }
    }

    public async Task<Dictionary<int, ValidationResultDto>> ValidateWithResultsAsync(DataTable dataTable, string tableType, CancellationToken cancellationToken = default)
    {
        var mapper = tableMappingService.GetMapper(tableType);
        if (mapper == null)
        {
            logger.LogError("No mapper found for table type: {TableType}", tableType);
            return new Dictionary<int, ValidationResultDto>();
        }

        var validationResults = new Dictionary<int, ValidationResultDto>();
        
        logger.LogInformation("Starting detailed validation for {TotalRecords} records", dataTable.Rows.Count);

        // Use batch validation if supported for better performance
        if (mapper is IBatchTableMapper batchMapper)
        {
            logger.LogInformation("Using optimized batch validation for {TotalRecords} records", dataTable.Rows.Count);
            
            try
            {
                validationResults = await batchMapper.ValidateBatchAsync(dataTable, cancellationToken);
                
                logger.LogInformation("Batch validation completed. Valid: {ValidRecords}, Invalid: {InvalidRecords}", 
                    validationResults.Count(vr => vr.Value.IsValid), validationResults.Count(vr => !vr.Value.IsValid));
                
                return validationResults;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Batch validation failed, falling back to row-by-row validation");
                validationResults.Clear();
            }
        }
        
        // Fall back to row-by-row validation if batch not supported or failed
        logger.LogInformation("Using row-by-row validation for {TotalRecords} records", dataTable.Rows.Count);
        
        var rowNumber = 1;
        foreach (DataRow row in dataTable.Rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var validationResult = await mapper.ValidateRowAsync(row, rowNumber, cancellationToken);
            validationResults[rowNumber] = validationResult;
            rowNumber++;
        }

        return validationResults;
    }
}