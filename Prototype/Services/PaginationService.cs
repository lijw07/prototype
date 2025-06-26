using Prototype.Constants;
using Prototype.Services.Interfaces;

namespace Prototype.Services;

/// <summary>
/// Service for handling pagination logic
/// </summary>
public class PaginationService : IPaginationService
{
    public (int page, int pageSize, int skip) ValidatePaginationParameters(int page, int pageSize)
    {
        // Validate page number
        if (page < 1)
            page = ApplicationConstants.Pagination.DefaultPage;

        // Validate page size
        if (pageSize < 1 || pageSize > ApplicationConstants.Pagination.MaxPageSize)
            pageSize = ApplicationConstants.Pagination.DefaultPageSize;

        var skip = (page - 1) * pageSize;
        return (page, pageSize, skip);
    }

    public object CreatePaginatedResponse<T>(IEnumerable<T> data, int page, int pageSize, int totalCount)
    {
        return new
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = CalculateTotalPages(totalCount, pageSize)
        };
    }

    public int CalculateTotalPages(int totalCount, int pageSize)
    {
        if (pageSize <= 0) return 0;
        return (int)Math.Ceiling((double)totalCount / pageSize);
    }
}