using System.Data;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload;

/// <summary>
/// High-performance bulk insert service for large data migrations
/// Uses SQL Server bulk copy operations for maximum performance
/// </summary>
public interface IBulkInsertService
{
    /// <summary>
    /// Performs high-speed bulk insert using SQL Server BulkCopy
    /// </summary>
    /// <param name="dataTable">DataTable containing the data to insert</param>
    /// <param name="tableName">Target database table name</param>
    /// <param name="columnMappings">Column mappings between DataTable and database table</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records inserted</returns>
    Task<Result<int>> BulkInsertAsync(DataTable dataTable, string tableName, 
        Dictionary<string, string> columnMappings, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs bulk upsert (insert or update) operation
    /// </summary>
    /// <param name="dataTable">DataTable containing the data</param>
    /// <param name="tableName">Target database table name</param>
    /// <param name="keyColumns">Primary key columns for upsert logic</param>
    /// <param name="columnMappings">Column mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records affected</returns>
    Task<Result<int>> BulkUpsertAsync(DataTable dataTable, string tableName, 
        List<string> keyColumns, Dictionary<string, string> columnMappings, 
        CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Checks if bulk operations are supported for the current database connection
    /// </summary>
    bool IsBulkOperationSupported { get; }
}