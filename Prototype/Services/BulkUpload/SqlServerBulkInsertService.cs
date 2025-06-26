using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload;

/// <summary>
/// High-performance SQL Server bulk insert implementation
/// Uses SqlBulkCopy for maximum insert performance
/// </summary>
public class SqlServerBulkInsertService : IBulkInsertService
{
    private readonly SentinelContext _context;
    private readonly ILogger<SqlServerBulkInsertService> _logger;

    public SqlServerBulkInsertService(SentinelContext context, ILogger<SqlServerBulkInsertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public bool IsBulkOperationSupported => _context.Database.IsSqlServer();

    public async Task<Result<int>> BulkInsertAsync(DataTable dataTable, string tableName, 
        Dictionary<string, string> columnMappings, CancellationToken cancellationToken = default)
    {
        if (!IsBulkOperationSupported)
        {
            return Result<int>.Failure("Bulk operations are only supported for SQL Server");
        }

        try
        {
            var connection = _context.Database.GetDbConnection() as SqlConnection;
            if (connection == null)
            {
                return Result<int>.Failure("Invalid SQL Server connection");
            }

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var transaction = connection.BeginTransaction();
            
            try
            {
                _context.Database.UseTransaction(transaction);
                
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, transaction)
                {
                    DestinationTableName = tableName,
                    BatchSize = CalculateOptimalBatchSize(dataTable.Rows.Count),
                    BulkCopyTimeout = 600, // 10 minutes timeout for large datasets
                    EnableStreaming = true,
                    NotifyAfter = Math.Max(1000, dataTable.Rows.Count / 20) // Dynamic progress notifications
                };

                // Add column mappings
                foreach (var mapping in columnMappings)
                {
                    bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                }

                // Progress reporting for large datasets
                bulkCopy.SqlRowsCopied += (sender, e) =>
                {
                    _logger.LogDebug("Bulk copy progress: {RowsCopied} rows inserted", e.RowsCopied);
                };

                _logger.LogInformation("Starting bulk insert of {RowCount} rows into {TableName}", 
                    dataTable.Rows.Count, tableName);

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully bulk inserted {RowCount} rows into {TableName}", 
                    dataTable.Rows.Count, tableName);

                return Result<int>.Success(dataTable.Rows.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error during bulk insert to {TableName}", tableName);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk insert failed for table {TableName}", tableName);
            return Result<int>.Failure($"Bulk insert failed: {ex.Message}");
        }
    }

    public async Task<Result<int>> BulkUpsertAsync(DataTable dataTable, string tableName, 
        List<string> keyColumns, Dictionary<string, string> columnMappings, 
        CancellationToken cancellationToken = default)
    {
        if (!IsBulkOperationSupported)
        {
            return Result<int>.Failure("Bulk operations are only supported for SQL Server");
        }

        try
        {
            // Create a temporary table for staging
            var tempTableName = $"#{tableName}_Temp_{Guid.NewGuid():N}";
            
            var connection = _context.Database.GetDbConnection() as SqlConnection;
            if (connection == null)
            {
                return Result<int>.Failure("Invalid SQL Server connection");
            }

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var transaction = connection.BeginTransaction();
            
            try
            {
                _context.Database.UseTransaction(transaction);
                
                // Create temporary table with same structure as target table
                var createTempTableSql = await GenerateCreateTempTableSql(tableName, tempTableName, connection, transaction);
                using var createCmd = new SqlCommand(createTempTableSql, connection, transaction);
                await createCmd.ExecuteNonQueryAsync(cancellationToken);

                // Bulk insert into temporary table
                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                {
                    DestinationTableName = tempTableName,
                    BatchSize = 5000,
                    BulkCopyTimeout = 300,
                    EnableStreaming = true
                };

                foreach (var mapping in columnMappings)
                {
                    bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                }

                await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);

                // Perform MERGE operation for upsert
                var mergeSql = GenerateMergeSql(tableName, tempTableName, keyColumns, columnMappings.Values.ToList());
                using var mergeCmd = new SqlCommand(mergeSql, connection, transaction);
                mergeCmd.CommandTimeout = 300;
                
                var affectedRows = await mergeCmd.ExecuteNonQueryAsync(cancellationToken);
                
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Successfully bulk upserted {AffectedRows} rows in {TableName}", 
                    affectedRows, tableName);

                return Result<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error during bulk upsert to {TableName}", tableName);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk upsert failed for table {TableName}", tableName);
            return Result<int>.Failure($"Bulk upsert failed: {ex.Message}");
        }
    }

    private async Task<string> GenerateCreateTempTableSql(string sourceTable, string tempTable, 
        SqlConnection connection, SqlTransaction transaction)
    {
        var sql = $@"
            SELECT TOP 0 * 
            INTO {tempTable} 
            FROM {sourceTable}";
        
        return sql;
    }

    private int CalculateOptimalBatchSize(int totalRecords)
    {
        // Optimize batch size based on record count for SQL Server bulk operations
        return totalRecords switch
        {
            < 1000 => 500,
            < 10000 => 2000,
            < 50000 => 5000,
            < 200000 => 10000,
            _ => 20000 // Maximum for very large datasets
        };
    }

    private string GenerateMergeSql(string targetTable, string sourceTable, 
        List<string> keyColumns, List<string> allColumns)
    {
        var keyJoinConditions = string.Join(" AND ", 
            keyColumns.Select(col => $"target.{col} = source.{col}"));
        
        var updateColumns = allColumns.Where(col => !keyColumns.Contains(col))
            .Select(col => $"target.{col} = source.{col}");
        
        var insertColumns = string.Join(", ", allColumns);
        var insertValues = string.Join(", ", allColumns.Select(col => $"source.{col}"));

        return $@"
            MERGE {targetTable} AS target
            USING {sourceTable} AS source
            ON {keyJoinConditions}
            WHEN MATCHED THEN
                UPDATE SET {string.Join(", ", updateColumns)}
            WHEN NOT MATCHED THEN
                INSERT ({insertColumns})
                VALUES ({insertValues});";
    }
}