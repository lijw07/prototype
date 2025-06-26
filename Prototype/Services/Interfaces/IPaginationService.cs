namespace Prototype.Services.Interfaces;

/// <summary>
/// Provides pagination functionality for data queries
/// </summary>
public interface IPaginationService
{
    /// <summary>
    /// Validates and normalizes pagination parameters
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Validated page, pageSize, and calculated skip values</returns>
    (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize);

    /// <summary>
    /// Creates a paginated response object
    /// </summary>
    /// <typeparam name="T">Type of data items</typeparam>
    /// <param name="data">The data items for current page</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="totalCount">Total number of items</param>
    /// <returns>Paginated response object</returns>
    object CreatePaginatedResponse<T>(IEnumerable<T> data, int page, int pageSize, int totalCount);

    /// <summary>
    /// Calculates the total number of pages
    /// </summary>
    /// <param name="totalCount">Total number of items</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Total number of pages</returns>
    int CalculateTotalPages(int totalCount, int pageSize);
}