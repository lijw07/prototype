using System.Data;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload.Mappers;

public class ApplicationTableMapper(
    IServiceScopeFactory scopeFactory,
    SentinelContext context,
    ILogger<ApplicationTableMapper> logger)
    : ITableMapper
{
    public string TableType => "Applications";

    public async Task<ValidationResultDto> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResultDto { IsValid = true };

        try
        {
            var applicationName = GetColumnValue(row, "ApplicationName");
            var applicationDescription = GetColumnValue(row, "ApplicationDescription");
            var dataSourceType = GetColumnValue(row, "ApplicationDataSourceType");

            if (string.IsNullOrWhiteSpace(applicationName))
                result.Errors.Add($"Row {rowNumber}: ApplicationName is required");

            if (string.IsNullOrWhiteSpace(applicationDescription))
                result.Errors.Add($"Row {rowNumber}: ApplicationDescription is required");

            if (!string.IsNullOrWhiteSpace(applicationName) && applicationName.Length > 100)
                result.Errors.Add($"Row {rowNumber}: ApplicationName cannot exceed 100 characters");

            if (!string.IsNullOrWhiteSpace(applicationDescription) && applicationDescription.Length > 500)
                result.Errors.Add($"Row {rowNumber}: ApplicationDescription cannot exceed 500 characters");

            if (!string.IsNullOrWhiteSpace(dataSourceType) && !IsValidDataSourceType(dataSourceType))
                result.Errors.Add($"Row {rowNumber}: Invalid ApplicationDataSourceType. Must be Database, API, File, or Cloud");

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
            
            if (!string.IsNullOrWhiteSpace(applicationName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existingApp = await context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationName == applicationName, cancellationToken);
                if (existingApp != null)
                    result.Errors.Add($"Row {rowNumber}: ApplicationName '{applicationName}' already exists");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating application row {RowNumber}", rowNumber);
            result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var dataSourceTypeStr = GetColumnValue(row, "ApplicationDataSourceType") ?? "Database";
            var dataSourceType = System.Enum.Parse<Prototype.Enum.DataSourceTypeEnum>(dataSourceTypeStr);
            
            var application = new ApplicationModel
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationName = GetColumnValue(row, "ApplicationName"),
                ApplicationDescription = GetColumnValue(row, "ApplicationDescription"),
                ApplicationDataSourceType = dataSourceType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Applications.Add(application);
            
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving application row");
            return Task.FromResult(Result<bool>.Failure($"Error saving application: {ex.Message}"));
        }
    }

    public List<TableColumnInfoDto> GetTemplateColumns()
    {
        return new List<TableColumnInfoDto>
        {
            new() { ColumnName = "ApplicationName", DataType = "string", IsRequired = true, MaxLength = 100, Description = "Unique name for the application" },
            new() { ColumnName = "ApplicationDescription", DataType = "string", IsRequired = true, MaxLength = 500, Description = "Description of the application" },
            new() { ColumnName = "ApplicationDataSourceType", DataType = "string", IsRequired = false, DefaultValue = "Database", Description = "Data source type: Database, API, File, or Cloud" },
            new() { ColumnName = "IsActive", DataType = "boolean", IsRequired = false, DefaultValue = "true", Description = "Whether the application is active" },
            new() { ColumnName = "Owner", DataType = "string", IsRequired = false, MaxLength = 100, Description = "Application owner or contact person" },
            new() { ColumnName = "Category", DataType = "string", IsRequired = false, MaxLength = 50, Description = "Application category" }
        };
    }

    public List<Dictionary<string, object>> GetExampleData()
    {
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["ApplicationName"] = "Employee Portal",
                ["ApplicationDescription"] = "Internal employee self-service portal",
                ["ApplicationDataSourceType"] = "Database",
                ["IsActive"] = true,
                ["Owner"] = "HR Department",
                ["Category"] = "Human Resources"
            },
            new()
            {
                ["ApplicationName"] = "Inventory Management",
                ["ApplicationDescription"] = "System for tracking inventory and supplies",
                ["ApplicationDataSourceType"] = "API",
                ["IsActive"] = true,
                ["Owner"] = "Operations Team",
                ["Category"] = "Operations"
            }
        };
    }

    private string GetColumnValue(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
            return string.Empty;

        var value = row[columnName];
        return value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
    }

    private bool IsValidDataSourceType(string dataSourceType)
    {
        var validTypes = new[] { "Database", "API", "File", "Cloud" };
        return validTypes.Contains(dataSourceType, StringComparer.OrdinalIgnoreCase);
    }
}