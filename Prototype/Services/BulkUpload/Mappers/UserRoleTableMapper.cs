using System.Data;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload.Mappers;

public class UserRoleTableMapper(
    IServiceScopeFactory scopeFactory,
    SentinelContext context,
    ILogger<UserRoleTableMapper> logger)
    : IBatchTableMapper
{
    public string TableType => "UserRoles";

    public async Task<ValidationResultDto> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResultDto { IsValid = true };

        try
        {
            var role = GetColumnValue(row, "Role");
            var createdBy = GetColumnValue(row, "CreatedBy");

            // Required field validation
            if (string.IsNullOrWhiteSpace(role))
                result.Errors.Add($"Row {rowNumber}: Role is required");

            if (string.IsNullOrWhiteSpace(createdBy))
                result.Errors.Add($"Row {rowNumber}: CreatedBy is required");

            // Format validation
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (role.Length > 100)
                    result.Errors.Add($"Row {rowNumber}: Role name cannot exceed 100 characters");

                if (role.Trim() != role)
                    result.Errors.Add($"Row {rowNumber}: Role name cannot have leading or trailing whitespace");
            }

            if (!string.IsNullOrWhiteSpace(createdBy) && createdBy.Length > 50)
                result.Errors.Add($"Row {rowNumber}: CreatedBy cannot exceed 50 characters");

            // NOTE: Individual row validation skips database checks - use batch validation for performance
            // Database duplicate checking is done in ValidateBatchAsync to avoid N+1 queries

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating user role row {RowNumber}", rowNumber);
            result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for cancellation before processing
            cancellationToken.ThrowIfCancellationRequested();

            var userRole = new UserRoleModel
            {
                UserRoleId = Guid.NewGuid(),
                RoleName = GetColumnValue(row, "Role").Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetColumnValue(row, "CreatedBy")
            };

            context.UserRoles.Add(userRole);
            // Note: SaveChanges will be called by the service after all rows are processed

            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user role row");
            return Task.FromResult(Result<bool>.Failure($"Error creating user role: {ex.Message}"));
        }
    }

    public List<TableColumnInfoDto> GetTemplateColumns()
    {
        return new List<TableColumnInfoDto>
        {
            new() { ColumnName = "Role", DataType = "string", IsRequired = true, MaxLength = 100, Description = "Name of the user role (must be unique)" },
            new() { ColumnName = "CreatedBy", DataType = "string", IsRequired = true, MaxLength = 50, Description = "Username of who is creating this role" }
        };
    }

    public List<Dictionary<string, object>> GetExampleData()
    {
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["Role"] = "Data Analyst",
                ["CreatedBy"] = "system.admin"
            },
            new()
            {
                ["Role"] = "Project Manager",
                ["CreatedBy"] = "hr.manager"
            },
            new()
            {
                ["Role"] = "Developer",
                ["CreatedBy"] = "system.admin"
            },
            new()
            {
                ["Role"] = "QA Tester",
                ["CreatedBy"] = "team.lead"
            }
        };
    }

    public async Task<Dictionary<int, ValidationResultDto>> ValidateBatchAsync(DataTable dataTable, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, ValidationResultDto>();

        try
        {
            // Pre-load existing data for efficient lookups
            var allRoles = dataTable.Rows.Cast<DataRow>()
                .Select(row => GetColumnValue(row, "Role"))
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Batch database queries
            var existingRolesData = await context.UserRoles
                .Where(ur => allRoles.Contains(ur.RoleName))
                .Select(ur => ur.RoleName.ToLower())
                .ToListAsync(cancellationToken);
            var existingRoles = new HashSet<string>(existingRolesData);

            // Track duplicates within the batch
            var rolesInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Process each row
            var rowNumber = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                var result = new ValidationResultDto { IsValid = true };

                var role = GetColumnValue(row, "Role");
                var createdBy = GetColumnValue(row, "CreatedBy");

                // Validate required fields
                if (string.IsNullOrWhiteSpace(role))
                    result.Errors.Add($"Row {rowNumber}: Role is required");

                if (string.IsNullOrWhiteSpace(createdBy))
                    result.Errors.Add($"Row {rowNumber}: CreatedBy is required");

                if (!string.IsNullOrWhiteSpace(role))
                {
                    // Validate format
                    if (role.Length > 100)
                        result.Errors.Add($"Row {rowNumber}: Role name cannot exceed 100 characters");

                    if (role.Trim() != role)
                        result.Errors.Add($"Row {rowNumber}: Role name cannot have leading or trailing whitespace");

                    // Check for duplicates in existing data
                    if (existingRoles.Contains(role.ToLower()))
                        result.Errors.Add($"Row {rowNumber}: Role '{role}' already exists (DUPLICATE)");

                    // Check for duplicates within batch
                    if (rolesInBatch.Contains(role))
                        result.Errors.Add($"Row {rowNumber}: Role '{role}' appears multiple times in this batch");
                    else
                        rolesInBatch.Add(role);
                }

                if (!string.IsNullOrWhiteSpace(createdBy) && createdBy.Length > 50)
                    result.Errors.Add($"Row {rowNumber}: CreatedBy cannot exceed 50 characters");

                result.IsValid = !result.Errors.Any();
                results[rowNumber] = result;
                rowNumber++;
            }

            logger.LogInformation("Batch validation completed for {RowCount} rows. Valid: {ValidCount}, Invalid: {InvalidCount}",
                results.Count, results.Count(r => r.Value.IsValid), results.Count(r => !r.Value.IsValid));

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch validation");
            throw;
        }
    }

    public async Task<Result<int>> SaveBatchAsync(DataTable dataTable, Guid userId, Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var rolesToAdd = new List<UserRoleModel>();
            var rowNumber = 1;
            var successCount = 0;

            foreach (DataRow row in dataTable.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Skip invalid rows if not ignoring errors
                if (validationResults.ContainsKey(rowNumber) && !validationResults[rowNumber].IsValid && !ignoreErrors)
                {
                    rowNumber++;
                    continue;
                }

                var userRole = new UserRoleModel
                {
                    UserRoleId = Guid.NewGuid(),
                    RoleName = GetColumnValue(row, "Role"),
                    CreatedBy = GetColumnValue(row, "CreatedBy"),
                    CreatedAt = DateTime.UtcNow
                };

                rolesToAdd.Add(userRole);
                successCount++;
                rowNumber++;
            }

            // Bulk add all roles at once
            if (rolesToAdd.Any())
            {
                context.UserRoles.AddRange(rolesToAdd);
                logger.LogInformation("Added {RoleCount} user roles to context for bulk save", rolesToAdd.Count);
            }

            return Result<int>.Success(successCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch save");
            return Result<int>.Failure($"Batch save error: {ex.Message}");
        }
    }

    private string GetColumnValue(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
            return string.Empty;

        var value = row[columnName];
        return value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
    }
}